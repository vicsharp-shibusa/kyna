using Microsoft.Extensions.Logging;

namespace Kyna.Infrastructure.Logging.PostgreSQL;

internal sealed class Logger(LoggerProvider loggerProvider,
    string categoryName,
    Func<string, LogLevel, bool>? filter = null) : ILogger
{
    private LogScope? _scope = null;

    private readonly Func<string, LogLevel, bool>? _filter = filter;

    private readonly string _categoryName = string.IsNullOrWhiteSpace(categoryName)
        ? throw new ArgumentNullException(nameof(categoryName))
        : categoryName;

    private readonly LoggerProvider _loggerProvider = loggerProvider ?? throw new ArgumentNullException(nameof(loggerProvider));

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        _scope = new LogScope(state);
        return _scope;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None &&
        (_filter == null || _filter(_categoryName, logLevel));

    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState? state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        { return; }

        if (state is null && exception is null)
        {
            throw new ArgumentException($"{nameof(state)} and {nameof(exception)} can not both be null when logging.");
        }

        LogItem? logItem = null;

        if (state is not null)
        {
            logItem = state is LogItem
                ? state as LogItem
                : new LogItem(state.ToString(), logLevel, _scope?.ScopeMessage);
        }

        EventId evId = logItem is not null && !logItem.EventId.Equals(default) ? logItem.EventId : eventId;

        Database.DataAccessObjects.Log? logDao = null;
        Database.DataAccessObjects.AppEvent? appEventDao = null;

        if (logItem?.Message is not null)
        {
            logDao = new()
            {
                Message = logItem.Message,
                Exception = logItem.Exception?.ToString(),
                LogLevel = logItem.LogLevel.ToString(),
                Scope = logItem.Scope ?? _scope?.ScopeMessage,
                ProcessId = logItem.ProcessId
            };
        }

        if (!evId.Equals(default))
        {
            appEventDao = new()
            {
                EventId = evId.Id,
                EventName = evId.Name,
                ProcessId = logItem?.ProcessId
            };
        }


        if (logDao is not null)
        {
            _loggerProvider.PreserveLog(logDao);
        }

        if (appEventDao is not null)
        {
            _loggerProvider.PreserveLog(appEventDao);
        }
    }

    private class LogScope(object? state) : IDisposable
    {
        public string? ScopeMessage { get; protected set; } = state?.ToString();

        public void Dispose()
        {
            ScopeMessage = null;
        }
    }
}