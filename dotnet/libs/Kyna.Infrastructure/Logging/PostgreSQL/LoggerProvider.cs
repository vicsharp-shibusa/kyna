using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Kyna.Infrastructure.Logging.PostgreSQL;

internal class LoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<Database.DataAccessObjects.Log> _logQueue;
    private readonly ConcurrentQueue<Database.DataAccessObjects.AppEvent> _appEventQueue;

    private bool _disposedValue;

    private bool _runQueues = true;

    private readonly Func<string, LogLevel, bool>? _filter;

    private readonly PostgreSqlContext _dbContext;

    public LoggerProvider(string connectionString, Func<string, LogLevel, bool>? filter = null)
    {
        _dbContext = new PostgreSqlContext(new DbDef("Logs", DatabaseEngine.PostgreSql, connectionString));

        _filter = filter;

        _logQueue = new ConcurrentQueue<Database.DataAccessObjects.Log>();
        _appEventQueue = new ConcurrentQueue<Database.DataAccessObjects.AppEvent>();

        _runQueues = true;

        RunLoggerDequeue();
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName, _filter);

    internal void PreserveLog(Database.DataAccessObjects.Log? logDao)
    {
        if (logDao is not null)
        {
            _logQueue.Enqueue(logDao);
        }
    }

    internal void PreserveLog(Database.DataAccessObjects.AppEvent appEventDao)
    {
        if (appEventDao is not null)
        {
            _appEventQueue.Enqueue(appEventDao);
        }
    }

    private void RunLoggerDequeue()
    {
        Task.Run(() =>
        {
            while (_runQueues)
            {
                if (_logQueue.TryDequeue(out Database.DataAccessObjects.Log? logItem))
                {
                    if (logItem is not null)
                    {
                        _dbContext.Execute(_dbContext.Sql.Logs.Insert, logItem);
                    }
                }
            }
        });

        Task.Run(() =>
        {
            while (_runQueues)
            {
                if (_appEventQueue.TryDequeue(out Database.DataAccessObjects.AppEvent? appEvent))
                {
                    if (appEvent is not null)
                    {
                        _dbContext.Execute(_dbContext.Sql.AppEvents.Insert, appEvent);
                    }
                }
            }
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            const int NumberOfCycles = 10;
            const int MsSleepTime = 20;

            if (disposing)
            {
                _runQueues = false;

                int i = 0;

                while (!_logQueue.IsEmpty && i++ < NumberOfCycles)
                {
                    Thread.Sleep(MsSleepTime);
                }

                i = 0;

                while (!_appEventQueue.IsEmpty && i++ < NumberOfCycles)
                {
                    Thread.Sleep(MsSleepTime);
                }

            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
