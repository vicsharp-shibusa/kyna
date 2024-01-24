namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    public string InsertLog => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"
INSERT INTO public.logs (timestamp_utc, process_id, log_level, message, exception, scope)
VALUES(@TimestampUtc, @ProcessId, @LogLevel, @Message, @Exception, @Scope)",
        _ => ThrowSqlNotImplemented()
    };

    public string FetchLogs => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"
SELECT timestamp_utc AS TimestampUtc, process_id AS ProcessId,
log_level AS LogLevel, message, exception, scope
FROM public.logs",
        _ => ThrowSqlNotImplemented()
    };

    public string DeleteLogs => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"DELETE FROM public.logs",
        _ => ThrowSqlNotImplemented()
    };

    public string InsertAppEvent => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"
INSERT INTO public.app_events(timestamp_utc, process_id, event_id, event_name)
VALUES(@TimestampUtc, @ProcessId, @EventId, @EventName)",
        _ => ThrowSqlNotImplemented()
    };

    public string FetchAppEvents => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"
SELECT id, timestamp_utc AS TimestampUtc, process_id AS ProcessID,
event_id AS EventId, event_name AS EventName
FROM public.app_events",
        _ => ThrowSqlNotImplemented()
    };

    public string DeleteAppEvents => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"DELETE FROM public.app_events",
        _ => ThrowSqlNotImplemented()
    };
}
