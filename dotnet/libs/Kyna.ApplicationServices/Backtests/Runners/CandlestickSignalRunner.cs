using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Signals;
using Kyna.Analysis.Technical.Trends;
using Kyna.ApplicationServices.Analysis;
using Kyna.Backtests;
using Kyna.Common;
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
    private readonly ConcurrentQueue<SignalMatch> _queue;
    private bool _runQueue = true;
    private readonly CandlestickSignal[] _signals;

    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions()
    {
        ExpirationScanFrequency = TimeSpan.FromSeconds(30),
    });

    public CandlestickSignalRunner(DbDef finDef, DbDef backtestsDef,
        BacktestingConfiguration configuration,
        IEnumerable<CandlestickSignal> candlestickSignals,
        Guid? processId = null)
        : base(finDef, backtestsDef, configuration, processId)
    {
        if (configuration.Type != BacktestType.CandlestickPattern)
        {
            throw new ArgumentException($"Mismatch on backtesting type; should be {configuration.Type.GetEnumDescription()}");
        }
        _signals = candlestickSignals.ToArray();
        _financialsRepository = new(finDef);
        _queue = new();
        RunDequeue();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.Assert(_finDbContext != null);

        var codesAndCountsTask = GetCodesAndCount(cancellationToken);

        await CreateBacktestingRecord(cancellationToken).ConfigureAwait(false);

        OnCommunicate(new CommunicationEventArgs("Backtesting record created.", nameof(CandlestickSignalRunner)));

        if (_configuration.MaxParallelization.GetValueOrDefault() < 2)
        {
            foreach (var item in await codesAndCountsTask.ConfigureAwait(false))
            {
                ProcessCodesAndCounts(item);
            }
        }
        else
        {
            Parallel.ForEach(await codesAndCountsTask.ConfigureAwait(false), new ParallelOptions()
            {
                MaxDegreeOfParallelism = _configuration.MaxParallelization.GetValueOrDefault()
            }, ProcessCodesAndCounts);
        }

        _runQueue = false;
        while (!_queue.IsEmpty)
        {
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        await WaitForQueueAsync().ConfigureAwait(false);

        await ProcessStatsAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ProcessCodesAndCounts(CodesAndCounts item)
    {
        var ohlc = _financialsRepository.GetOhlcForSourceAndCodeAsync(
            _configuration.Source, item.Code).GetAwaiter().GetResult().ToArray();

        var chart = new Chart(item.Code, item.Industry, item.Sector)
            .WithCandles(ohlc)
            .WithTrend(new MovingAverageTrend(new MovingAverageKey(21), ohlc))
            .Build();

        Debug.Assert(!string.IsNullOrWhiteSpace(chart.Code));

        _memoryCache.Set(chart.Code, chart, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });

        foreach (var signal in _signals)
        {
            foreach (var match in signal.DiscoverMatches(chart).ToArray())
            {
                _queue.Enqueue(match);
            }
        }
    }

    private async Task ProcessStatsAsync(CancellationToken cancellationToken)
    {
        var repo = new CandlestickSignalRepository(new SignalOptions());

        var backtestResults = await _backtestDbContext.QueryAsync<BacktestResultsInfo>(
            _backtestDbContext.Sql.Backtests.FetchBacktestResultInfo,
            new { BacktestId = _backtestId }, cancellationToken: cancellationToken);

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
                TrendSentiment.Bullish => _configuration.TargetUp.Value * 100D,
                TrendSentiment.Bearish => _configuration.TargetDown.Value * 100D,
                _ => 0D
            };

            string criterion = $"{successResult} {percentage}%";

            var signalSubset = backtestResults.Where(b => b.SignalName.Equals(name)).ToArray();

            var numberEntities = signalSubset.Select(s => s.Code).Distinct().Count();

            var matchingResults = signalSubset.Where(s => s.ResultDirection == successResult).ToArray();

            double ratio = matchingResults.Length / (double)signalSubset.Length;

            await _backtestDbContext.ExecuteAsync(_backtestDbContext.Sql.Backtests.UpsertBacktestStats,
                    new BacktestStats(_configuration.Source,
                    name, "Overall", "All", numberEntities, signalSubset.Length, ratio, criterion,
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationTradingDays)),
                    Convert.ToInt32(matchingResults.Average(r => r.ResultDurationCalendarDays)),
                    DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId), cancellationToken: cancellationToken);

            var tickers = signalSubset.Select(b => b.Code).Distinct().ToArray();

            List<Task> tasks = new(tickers.Length);
            foreach (var ticker in tickers)
            {
                OnCommunicate(new CommunicationEventArgs($"Processing stats for {ticker}",
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
                    new BacktestStats(_configuration.Source,
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

                OnCommunicate(new CommunicationEventArgs($"Processing stats for {industry}",
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
                    new BacktestStats(_configuration.Source,
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
                OnCommunicate(new CommunicationEventArgs($"Processing stats for {sector}",
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
                    new BacktestStats(_configuration.Source,
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
                    if (_queue.TryDequeue(out SignalMatch signalMatch))
                    {
                        if (!_memoryCache.TryGetValue(signalMatch.Code, out Chart? chart) || chart == null)
                        {
                            throw new Exception($"Unable to pull chart for {signalMatch.Code} out of cache.");
                        }

                        var price = chart.PriceActions[signalMatch.Signal.End].GetPricePoint(_configuration.EntryPricePoint);

                        var targetUpPrice = price * (1M + (decimal)Math.Abs(_configuration.TargetUp.Value));
                        var targetDownPrice = price * (1M - (decimal)Math.Abs(_configuration.TargetDown.Value));

                        var upAction = chart.PriceActions.Skip(signalMatch.Signal.End + 1).FirstOrDefault(x => x.GetPricePoint(
                            _configuration.TargetUp.PricePoint) >= targetUpPrice);
                        var downAction = chart.PriceActions.Skip(signalMatch.Signal.End + 1).FirstOrDefault(x => x.GetPricePoint(
                            _configuration.TargetDown.PricePoint) <= targetDownPrice);

                        var detail = new BacktestResultDetail()
                        {
                            BacktestId = _backtestId,
                            SignalName = signalMatch.SignalName,
                            Code = chart.Code,
                            Industry = chart.Industry,
                            Sector = chart.Sector,
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
                                Date = chart.PriceActions[signalMatch.Signal.End].Date,
                                PricePoint = _configuration.EntryPricePoint,
                                Price = chart.PriceActions[signalMatch.Signal.End].GetPricePoint(_configuration.EntryPricePoint)
                            }
                        };

                        Enqueue(detail);
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
