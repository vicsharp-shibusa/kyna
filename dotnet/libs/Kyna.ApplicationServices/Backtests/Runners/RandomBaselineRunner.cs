using Kyna.ApplicationServices.Analysis;
using Kyna.Backtests.AlphaModel;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Events;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal sealed class RandomBaselineRunner : RunnerBase, IBacktestRunner
{
    private readonly ConcurrentQueue<(BacktestingConfiguration Configuration, Guid BacktestId,
        string Code, string? Industry, string? Sector, int[] Positions)> _queue;
    private bool _runQueue = true;
    private readonly FinancialsRepository _financialsRepository;
    private readonly Task<IEnumerable<CodesAndCounts>> _codesAndCountsTask;

    public RandomBaselineRunner(DbDef finDef, DbDef backtestDef, string source)
        : base(finDef, backtestDef)
    {
        _codesAndCountsTask = GetCodesAndCount(source, CancellationToken.None);
        _financialsRepository = new(finDef);
        _queue = new();
        RunDequeue();
    }

    public Task ExecuteAsync(FileInfo configFile, CancellationToken cancellationToken) =>
        ExecuteAsync([configFile], cancellationToken);

    public async Task ExecuteAsync(FileInfo[] configFiles, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.Assert(_finDbContext != null);

        foreach (var configFile in configFiles)
        {
            var configuration = DeserializeConfigFile(configFile);

            var backtestId = await CreateBacktestingRecord(configuration, cancellationToken).ConfigureAwait(false);

            if (configuration.MaxParallelization.GetValueOrDefault() < 2)
            {
                foreach (var item in await _codesAndCountsTask.ConfigureAwait(false))
                {
                    ProcessCodesAndCounts(backtestId, configuration, item);
                }
            }
            else
            {
                Parallel.ForEach(await _codesAndCountsTask, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = configuration.MaxParallelization.GetValueOrDefault()
                }, (item) => ProcessCodesAndCounts(backtestId, configuration, item));
            }
        }

        _runQueue = false;
        while (!_queue.IsEmpty)
        {
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        await WaitForQueueAsync().ConfigureAwait(false);

    }

    private readonly Random _rnd = new(Guid.NewGuid().GetHashCode());

    private void ProcessCodesAndCounts(Guid backtestId,
        BacktestingConfiguration configuration, CodesAndCounts item)
    {
        // remove 2 from the end because it's less likely the positions at the end
        // will have the time to reach their targets.
        int slices = (item.Count / 10) - 2;
        int[] positions = new int[slices];
        for (int i = 0; i < slices; i++)
        {
            positions[i] = (i * 10) + _rnd.Next(0, 10);
        }
        _queue.Enqueue((configuration, backtestId, item.Code, item.Industry, item.Sector, positions));
    }

    private void RunDequeue()
    {
        Task.Run(() =>
        {
            while (_runQueue || !_queue.IsEmpty)
            {
                try
                {
                    if (_queue.TryDequeue(out (BacktestingConfiguration Configuration,
                        Guid BacktestId,
                        string Code, string? Industry, string? Sector, int[] Positions) result))
                    {
                        lock (_activityCounts)
                        {
                            _activityCounts.EntityCount++;
                        }

                        var ohlc = _financialsRepository.GetOhlcForSourceAndCodeAsync(
                            result.Configuration.Source, result.Code).GetAwaiter().GetResult().ToArray();

                        foreach (var p in result.Positions)
                        {
                            var price = ohlc[p].GetPricePoint(result.Configuration.EntryPricePoint);
                            var targetUpPrice = price * (1M + (decimal)Math.Abs(result.Configuration.TargetUp.Value));
                            var targetDownPrice = price * (1M - (decimal)Math.Abs(result.Configuration.TargetDown.Value));

                            var upAction = ohlc.Skip(p + 1).FirstOrDefault(x => x.GetPricePoint(
                                result.Configuration.TargetUp.PricePoint) >= targetUpPrice);
                            var downAction = ohlc.Skip(p + 1).FirstOrDefault(x => x.GetPricePoint(
                                result.Configuration.TargetDown.PricePoint) <= targetDownPrice);

                            var detail = new BacktestResultDetail()
                            {
                                BacktestId = result.BacktestId,
                                SignalName = "Random",
                                Code = result.Code,
                                Industry = result.Industry,
                                Sector = result.Sector,
                                Up = upAction == null ? null : new ResultDetail()
                                {
                                    Date = upAction.Date,
                                    Price = upAction.GetPricePoint(result.Configuration.TargetUp.PricePoint),
                                    PricePoint = result.Configuration.TargetUp.PricePoint,
                                },
                                Down = downAction == null ? null : new ResultDetail()
                                {
                                    Date = downAction.Date,
                                    Price = downAction.GetPricePoint(result.Configuration.TargetDown.PricePoint),
                                    PricePoint = result.Configuration.TargetDown.PricePoint
                                },
                                Entry = new ResultDetail()
                                {
                                    Date = ohlc[p].Date,
                                    PricePoint = result.Configuration.EntryPricePoint,
                                    Price = ohlc[p].GetPricePoint(result.Configuration.EntryPricePoint)
                                }
                            };

                            Enqueue(detail);
                        }
                    }
                }
                catch (Exception exc)
                {
                    OnCommunicate(new CommunicationEventArgs(exc.ToString(), null));
                }
            }
        });
    }
}
