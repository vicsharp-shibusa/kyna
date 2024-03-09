using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Collections.Concurrent;
using System.Text;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal abstract class RunnerBase
{
    protected readonly IDbContext _finDbContext;
    protected readonly IDbContext _backtestDbContext;
    protected readonly BacktestingConfiguration _configuration;
    protected readonly Guid? _processId;
    protected readonly Guid _backtestId = Guid.NewGuid();
    protected readonly ActivityCounts _activityCounts;

    private readonly ConcurrentQueue<BacktestResultDetail> _resultDetails;
    private readonly bool _runQueue = true;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public RunnerBase(DbDef? finDef, DbDef? backtestsDef, BacktestingConfiguration? configuration,
        Guid? processId = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _finDbContext = DbContextFactory.Create(finDef ?? throw new ArgumentNullException(nameof(finDef)));
        _backtestDbContext = DbContextFactory.Create(backtestsDef ?? throw new ArgumentNullException(nameof(backtestsDef)));
        _processId = processId;
        _resultDetails = new();
        _activityCounts = new();
        RunResultDetailDequeue();
    }

    public void WriteActivityCounts()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Process Id   : {_processId}");
        sb.AppendLine($"Backtest Id  : {_backtestId}");
        sb.AppendLine($"Entity count : {_activityCounts.EntityCount.ToString("#,##0").PadLeft(12, ' ')}");
        sb.AppendLine($"Event count  : {_activityCounts.EventCount.ToString("#,##0").PadLeft(12, ' ')}");

        Communicate?.Invoke(this, new CommunicationEventArgs(sb.ToString(), nameof(RunnerBase)));
    }

    protected virtual Task<IEnumerable<CodesAndCounts>> GetCodesAndCount(CancellationToken cancellationToken)
    {
        OnCommunicate(new CommunicationEventArgs("Fetching data to backtest ...", null));
        return _finDbContext.QueryAsync<CodesAndCounts>(
            _finDbContext.Sql.AdjustedEodPrices.FetchCodesAndCounts, new { _configuration.Source },
            0, cancellationToken);
    }

    protected virtual Task CreateBacktestingRecord(CancellationToken cancellationToken)
    {
        OnCommunicate(new CommunicationEventArgs("Creating backtest record...", null));
        return _backtestDbContext.ExecuteAsync(_finDbContext.Sql.Backtests.UpsertBacktest,
            new Backtest(_backtestId,
                _configuration.Name,
                _configuration.Type.GetEnumDescription(),
                _configuration.Source,
                _configuration.Description,
                _configuration.EntryPricePoint.GetEnumDescription(),
                _configuration.TargetUp.Value,
                _configuration.TargetUp.PricePoint.GetEnumDescription(),
                _configuration.TargetDown.Value,
                _configuration.TargetDown.PricePoint.GetEnumDescription(),
                DateTime.UtcNow.Ticks,
                DateTime.UtcNow.Ticks,
                _processId), cancellationToken: cancellationToken);
    }

    protected virtual void Enqueue(BacktestResultDetail detail)
    {
        _resultDetails.Enqueue(detail);
    }

    protected virtual async Task WaitForQueueAsync(int milliseconds = 1_000)
    {
        while (!_resultDetails.IsEmpty)
        {
            await Task.Delay(milliseconds).ConfigureAwait(false);
        }
    }

    protected virtual void OnCommunicate(CommunicationEventArgs e)
    {
        Communicate?.Invoke(this, e);
    }

    private void RunResultDetailDequeue()
    {
        Task.Run(() =>
        {
            while (_runQueue || !_resultDetails.IsEmpty)
            {
                if (_resultDetails.TryDequeue(out BacktestResultDetail? resultDetail))
                {
                    if (resultDetail != null)
                    {
                        Communicate?.Invoke(this, new CommunicationEventArgs(
                            $"{resultDetail.Code}\t{resultDetail.Entry.Date:yyyy-MM-dd}", null));
                        try
                        {
                            _backtestDbContext.Execute(_backtestDbContext.Sql.Backtests.UpsertBacktestResult,
                                new Infrastructure.Database.DataAccessObjects.BacktestResult(
                                    resultDetail.Id,
                                    resultDetail.BacktestId,
                                    resultDetail.SignalName,
                                    resultDetail.Code,
                                    resultDetail.Industry,
                                    resultDetail.Sector,
                                    resultDetail.Entry.Date.GetValueOrDefault(),
                                    resultDetail.Entry.PricePoint.GetEnumDescription(),
                                    resultDetail.Entry.Price,
                                    resultDetail.Up?.Date,
                                    resultDetail.Up?.PricePoint.GetEnumDescription(),
                                    resultDetail.Up?.Price,
                                    resultDetail.Down?.Date,
                                    resultDetail.Down?.PricePoint.GetEnumDescription(),
                                    resultDetail.Down?.Price,
                                    resultDetail.WinnerText,
                                    resultDetail.WinnerDurationTradingDays,
                                    resultDetail.WinnerDurationCalendarDays,
                                    DateTime.UtcNow.Ticks,
                                    DateTime.UtcNow.Ticks));

                            lock (_activityCounts)
                            {
                                _activityCounts.EventCount++;
                            }
                        }
                        catch (Exception exc)
                        {
                            Communicate?.Invoke(this, new CommunicationEventArgs(exc.ToString(), null));
                        }
                    }
                }
            }
        });
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
    protected struct CodesAndCounts
    {
        public string Code;
        public string? Industry;
        public string? Sector;
        public int Count;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

    protected class ActivityCounts
    {
        public int EventCount { get; set; }
        public int EntityCount { get; set; }
    }
}