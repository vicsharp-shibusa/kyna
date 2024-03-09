using Kyna.ApplicationServices.Analysis;
using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal sealed class RandomBaselineRunner : RunnerBase, IBacktestRunner
{
    private readonly ConcurrentQueue<(string Code, string? Industry, string? Sector, int[] Positions)> _queue;
    private bool _runQueue = true;
    private readonly FinancialsRepository _financialsRepository;

    public RandomBaselineRunner(DbDef finDef, DbDef backtestDef, BacktestingConfiguration configuration,
        Guid? processId = null)
        : base(finDef, backtestDef, configuration, processId)
    {
        if (configuration.Type != BacktestType.RandomBaseline)
        {
            throw new ArgumentException($"Mismatch on backtesting type; should be {configuration.Type.GetEnumDescription()}");
        }

        _financialsRepository = new(finDef);
        _queue = new();
        RunDequeue();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.Assert(_finDbContext != null);

        Random rnd = new(Guid.NewGuid().GetHashCode());

        var codesAndCountsTask = GetCodesAndCount(cancellationToken);

        await CreateBacktestingRecord(cancellationToken).ConfigureAwait(false);

        foreach (var item in await codesAndCountsTask.ConfigureAwait(false))
        {
            // remove 2 from the end because it's less likely the positions at the end
            // will have the time to reach their targets.
            int slices = (item.Count / 10) - 2;
            int[] positions = new int[slices];
            for (int i = 0; i < slices; i++)
            {
                positions[i] = (i * 10) + rnd.Next(0, 10);
            }
            _queue.Enqueue((item.Code, item.Industry, item.Sector, positions));
        }

        _runQueue = false;
        while (!_queue.IsEmpty)
        {
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        await WaitForQueueAsync().ConfigureAwait(false);
    }

    private void RunDequeue()
    {
        Task.Run(() =>
        {
            while (_runQueue || !_queue.IsEmpty)
            {
                try
                {
                    if (_queue.TryDequeue(out (string Code, string? Industry, string? Sector, int[] Positions) result))
                    {
                        lock (_activityCounts)
                        {
                            _activityCounts.EntityCount++;
                        }

                        var ohlc = _financialsRepository.GetOhlcForSourceAndCodeAsync(
                            _configuration.Source, result.Code).GetAwaiter().GetResult().ToArray();

                        foreach (var p in result.Positions)
                        {
                            var price = ohlc[p].GetPricePoint(_configuration.EntryPricePoint);
                            var targetUpPrice = price * (1M + (decimal)Math.Abs(_configuration.TargetUp.Value));
                            var targetDownPrice = price * (1M - (decimal)Math.Abs(_configuration.TargetDown.Value));

                            var upAction = ohlc.Skip(p + 1).FirstOrDefault(x => x.GetPricePoint(
                                _configuration.TargetUp.PricePoint) >= targetUpPrice);
                            var downAction = ohlc.Skip(p + 1).FirstOrDefault(x => x.GetPricePoint(
                                _configuration.TargetDown.PricePoint) <= targetDownPrice);

                            var detail = new BacktestResultDetail()
                            {
                                BacktestId = _backtestId,
                                SignalName = "Random",
                                Code = result.Code,
                                Industry = result.Industry,
                                Sector = result.Sector,
                                Up = upAction == null ? null : new ResultDetail()
                                {
                                    Date = upAction.Date,
                                    Price = upAction.GetPricePoint(_configuration.TargetUp.PricePoint),
                                    PricePoint = _configuration.TargetUp.PricePoint,
                                },
                                Down = downAction == null ? null : new ResultDetail()
                                {
                                    Date = downAction.Date,
                                    Price = downAction.GetPricePoint(_configuration.TargetDown.PricePoint),
                                    PricePoint = _configuration.TargetDown.PricePoint
                                },
                                Entry = new ResultDetail()
                                {
                                    Date = ohlc[p].Date,
                                    PricePoint = _configuration.EntryPricePoint,
                                    Price = ohlc[p].GetPricePoint(_configuration.EntryPricePoint)
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
