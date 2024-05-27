using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Signals;
using Kyna.ApplicationServices.Analysis;
using Kyna.Backtests;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal class CandlestickSignalRunner : RunnerBase, IBacktestRunner
{
    private readonly FinancialsRepository _financialsRepository;
    private readonly ConcurrentQueue<(BacktestingConfiguration Configuration,
        Guid BacktestId, SignalMatch SignalMatch)> _queue;
    private bool _runQueue = true;
    private readonly CandlestickSignal[] _signals;
    private readonly Task<IEnumerable<CodesAndCounts>> _codesAndCountsTask;

    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions()
    {
        ExpirationScanFrequency = TimeSpan.FromSeconds(30),
    });

    public CandlestickSignalRunner(DbDef finDef, DbDef backtestsDef, string source,
        IEnumerable<CandlestickSignal> candlestickSignals)
        : base(finDef, backtestsDef)
    {
        _codesAndCountsTask = GetCodesAndCount(source, CancellationToken.None);
        _signals = candlestickSignals.ToArray();
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
            WriteConfigInfo(configFile);

            var configuration = DeserializeConfigFile(configFile);

            var backtestId = await CreateBacktestingRecord(configuration, cancellationToken).ConfigureAwait(false);

            OnCommunicate(new CommunicationEventArgs("Backtesting record created.", nameof(CandlestickSignalRunner)));

            Chart? market = null;
            var len = configuration.MarketConfiguration?.Codes?.Length ?? 0;
            if (len > 0 && configuration.MarketConfiguration?.Trends != null)
            {
                List<Ohlc[]> ohlcsList = new(len);
                for (int i = 0; i < len; i++)
                {
                    var code = configuration.MarketConfiguration.Codes![i];
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        ohlcsList.Add((await _financialsRepository.GetOhlcForSourceAndCodeAsync(
                            configuration.Source, code)).ToArray());
                    }
                }
                market = ChartFactory.Create(configuration.Source, "Market", new ChartConfiguration()
                {
                    Interval = "Daily",
                    Trends = configuration.MarketConfiguration.Trends
                }, null, null, [.. ohlcsList]);
            }

            OnCommunicate(new CommunicationEventArgs("Fetching data to process ...", nameof(CandlestickSignalRunner)));

            if (configuration.MaxParallelization.GetValueOrDefault() < 2)
            {
                foreach (var item in await _codesAndCountsTask.ConfigureAwait(false))
                {
                    ProcessCodesAndCounts(backtestId, configuration, item, market);
                }
            }
            else
            {
                Parallel.ForEach(await _codesAndCountsTask.ConfigureAwait(false), new ParallelOptions()
                {
                    MaxDegreeOfParallelism = configuration.MaxParallelization.GetValueOrDefault()
                }, (item) => ProcessCodesAndCounts(backtestId, configuration, item, market));
            }

            while (!_queue.IsEmpty)
            {
                await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
            }

            await WaitForQueueAsync().ConfigureAwait(false);

            await ProcessStatsAsync(configuration, backtestId, cancellationToken).ConfigureAwait(false);
        }

        _runQueue = false;
    }

    private void ProcessCodesAndCounts(Guid backtestId,
        BacktestingConfiguration configuration, CodesAndCounts item, Chart? market = null)
    {
        var ohlc = _financialsRepository.GetOhlcForSourceAndCodeAsync(
            configuration.Source, item.Code).GetAwaiter().GetResult().ToArray();

        var chart = ChartFactory.Create(configuration.Source, item.Code, item.Industry, item.Sector,
            ohlc, configuration.ChartConfiguration);

        Debug.Assert(!string.IsNullOrWhiteSpace(chart.Code));

        _memoryCache.Set(chart.Code, ohlc, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });

        foreach (var signal in _signals)
        {
            foreach (var match in signal.DiscoverMatches(chart, market, configuration.OnlySignalWithMarket,
                configuration.VolumeFactor).ToArray())
            {
                _queue.Enqueue((configuration, backtestId, match));
            }
        }
    }

    private async Task ProcessStatsAsync(BacktestingConfiguration configuration,
        Guid backtestId,
        CancellationToken cancellationToken)
    {
        var repo = new CandlestickSignalRepository(new SignalOptions(configuration.LengthOfPrologue));

        var backtestResults = await _backtestDbContext.QueryAsync<BacktestResultsInfo>(
            _backtestDbContext.Sql.Backtests.FetchBacktestResultInfo,
            new { backtestId }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var signalNames = backtestResults.Select(b => b.SignalName).Distinct().ToArray();

        foreach (var name in signalNames)
        {
            var signal = repo.Find(name);
            if (signal == null)
            {
                continue;
            }

            var successResult = signal.Sentiment switch
            {
                TrendSentiment.Bullish => "Up",
                TrendSentiment.Bearish => "Down",
                _ => "Sideways"
            };

            var percentage = signal.Sentiment switch
            {
                TrendSentiment.Bullish => configuration.TargetUp.Value * 100D,
                TrendSentiment.Bearish => configuration.TargetDown.Value * 100D,
                _ => 0D
            };

            string criterion = $"{successResult} {percentage}%";

            var signalSubset = backtestResults.Where(b => b.SignalName.Equals(name)).ToArray();

            var numberEntities = signalSubset.Select(s => s.Code).Distinct().Count();

            var matchingResults = signalSubset.Where(s => s.ResultDirection == successResult).ToArray();

            double ratio = matchingResults.Length / (double)signalSubset.Length;

            await _backtestDbContext.ExecuteAsync(_backtestDbContext.Sql.Backtests.UpsertBacktestStats,
                    new BacktestStats(backtestId, configuration.Source,
                    name, "Overall", "All", numberEntities, signalSubset.Length, ratio, criterion,
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationTradingDays)),
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationCalendarDays)),
                    DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var tickers = signalSubset.Select(b => b.Code).Distinct().ToArray();

            List<Task> tasks = new(tickers.Length);
            foreach (var ticker in tickers)
            {
                OnCommunicate(new CommunicationEventArgs($"{name} - processing stats for {ticker}",
                    nameof(CandlestickSignalRunner)));
                var totalInstances = signalSubset.Count(s => s.Code.Equals(ticker));
                if (totalInstances == 0)
                {
                    continue;
                }
                matchingResults = signalSubset.Where(s => s.Code.Equals(ticker) &&
                    s.ResultDirection == successResult).ToArray();
                ratio = matchingResults.Length / (double)totalInstances;

                tasks.Add(_backtestDbContext.ExecuteAsync(_backtestDbContext.Sql.Backtests.UpsertBacktestStats,
                    new BacktestStats(backtestId, configuration.Source,
                    name, "Entity", ticker, 1, totalInstances, ratio, criterion,
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationTradingDays)),
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationCalendarDays)),
                    DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId), cancellationToken: cancellationToken));
            }
            Task.WaitAll([.. tasks], cancellationToken);

            var industries = signalSubset.Select(s => s.Industry).Distinct().ToArray();
            tasks = new(industries.Length);
            foreach (var industry in industries)
            {
                if (industry == null)
                {
                    continue;
                }

                OnCommunicate(new CommunicationEventArgs($"{name} - processing stats for {industry}",
                    nameof(CandlestickSignalRunner)));

                var categoryCount = backtestResults.Count(b => b.Industry != null &&
                    b.Industry.Equals(industry));
                var totalInstances = signalSubset.Count(s => s.Industry != null && s.Industry.Equals(industry));
                if (totalInstances == 0)
                {
                    continue;
                }
                matchingResults = signalSubset.Where(s => s.Industry != null &&
                    s.Industry.Equals(industry) && s.ResultDirection == successResult).ToArray();
                ratio = matchingResults.Length / (double)totalInstances;
                tasks.Add(_backtestDbContext.ExecuteAsync(_backtestDbContext.Sql.Backtests.UpsertBacktestStats,
                    new BacktestStats(backtestId, configuration.Source,
                    name, "Industry", industry, categoryCount, totalInstances, ratio, criterion,
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationTradingDays)),
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationCalendarDays)),
                    DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId), cancellationToken: cancellationToken));
            }
            Task.WaitAll([.. tasks], cancellationToken);

            var sectors = signalSubset.Select(s => s.Sector).Distinct().ToArray();
            tasks = new(sectors.Length);
            foreach (var sector in sectors)
            {
                if (sector == null)
                {
                    continue;
                }
                OnCommunicate(new CommunicationEventArgs($"{name} - processing stats for {sector}",
                    nameof(CandlestickSignalRunner)));
                var categoryCount = backtestResults.Count(b => b.Sector != null &&
                    b.Sector.Equals(sector));
                var totalInstances = signalSubset.Count(s => s.Sector != null && s.Sector.Equals(sector));
                if (totalInstances == 0)
                {
                    continue;
                }
                matchingResults = signalSubset.Where(s => s.Sector != null &&
                    s.Sector.Equals(sector) && s.ResultDirection == successResult).ToArray();
                ratio = matchingResults.Length / (double)totalInstances;
                tasks.Add(_backtestDbContext.ExecuteAsync(_backtestDbContext.Sql.Backtests.UpsertBacktestStats,
                    new BacktestStats(backtestId, configuration.Source,
                    name, "Sector", sector, categoryCount, totalInstances, ratio, criterion,
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationTradingDays)),
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationCalendarDays)),
                    DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId), cancellationToken: cancellationToken));
            }
            Task.WaitAll([.. tasks], cancellationToken);
        }
    }

    private void RunDequeue()
    {
        Task.Run(() =>
        {
            while (_runQueue || !_queue.IsEmpty)
            {
                try
                {
                    if (_queue.TryDequeue(out (BacktestingConfiguration Configuration, Guid BacktestId,
                        SignalMatch SignalMatch) result))
                    {
                        if (!_memoryCache.TryGetValue(result.SignalMatch.Code, out Ohlc[]? ohlc))
                        {
                            throw new Exception($"Unable to pull chart for {result.SignalMatch.Code} out of cache.");
                        }

                        var chart = ChartFactory.Create(result.Configuration.Source,
                            result.SignalMatch.Code,
                            result.SignalMatch.Industry, result.SignalMatch.Sector,
                            ohlc!, configuration: result.Configuration.ChartConfiguration);

                        var price = chart.PriceActions[result.SignalMatch.Position].GetPricePoint(result.Configuration.EntryPricePoint);

                        var targetUpPrice = price * (1M + (decimal)Math.Abs(result.Configuration.TargetUp.Value));
                        var targetDownPrice = price * (1M - (decimal)Math.Abs(result.Configuration.TargetDown.Value));

                        var upAction = chart.PriceActions.Skip(result.SignalMatch.Position + 1).FirstOrDefault(x => x.GetPricePoint(
                            result.Configuration.TargetUp.PricePoint) >= targetUpPrice);
                        var downAction = chart.PriceActions.Skip(result.SignalMatch.Position + 1).FirstOrDefault(x => x.GetPricePoint(
                            result.Configuration.TargetDown.PricePoint) <= targetDownPrice);

                        var detail = new BacktestResultDetail()
                        {
                            BacktestId = result.BacktestId,
                            SignalName = result.SignalMatch.SignalName,
                            Code = chart.Code ?? "UNKNOWN",
                            Industry = chart.Industry,
                            Sector = chart.Sector,
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
                                Date = chart.PriceActions[result.SignalMatch.Position].Date,
                                PricePoint = result.Configuration.EntryPricePoint,
                                Price = chart.PriceActions[result.SignalMatch.Position].GetPricePoint(result.Configuration.EntryPricePoint)
                            }
                        };

                        Enqueue(detail);
                        chart = null;
                        ohlc = null;
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
