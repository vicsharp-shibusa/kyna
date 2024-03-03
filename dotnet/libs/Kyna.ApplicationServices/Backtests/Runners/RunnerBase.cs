using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using System.Collections.Concurrent;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal abstract class RunnerBase
{
    protected readonly IDbContext _finDbContext;
    protected readonly IDbContext _backtestDbContext;
    protected readonly BacktestingConfiguration _configuration;
    protected readonly Guid? _processId;
    private readonly ConcurrentQueue<BacktestResultDetail> _resultDetails;

    private readonly bool _runQueue = true;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public RunnerBase(DbDef? finDef, DbDef? backtestsDef, BacktestingConfiguration? configuration,
        Guid? processId = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _processId = processId;
        _finDbContext = DbContextFactory.Create(finDef ?? throw new ArgumentNullException(nameof(finDef)));
        _backtestDbContext = DbContextFactory.Create(backtestsDef ?? throw new ArgumentNullException(nameof(backtestsDef)));
        _resultDetails = new();
        RunResultDetailDequeue();
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
}