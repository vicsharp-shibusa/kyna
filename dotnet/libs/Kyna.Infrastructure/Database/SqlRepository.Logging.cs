namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    internal class LogsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Insert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
INSERT INTO public.logs (ticks_utc, process_id, log_level, message, exception, scope)
VALUES(@TicksUtc, @ProcessId, @LogLevel, @Message, @Exception, @Scope)",
            _ => ThrowSqlNotImplemented()
        };

        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
SELECT ticks_utc AS TicksUtc, process_id AS ProcessId,
log_level AS LogLevel, message, exception, scope
FROM public.logs",
            _ => ThrowSqlNotImplemented()
        };

        public string Delete => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.logs",
            _ => ThrowSqlNotImplemented()
        };
    }

    internal class EventsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Insert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
INSERT INTO public.app_events(ticks_utc, process_id, event_id, event_name)
VALUES(@TicksUtc, @ProcessId, @EventId, @EventName)",
            _ => ThrowSqlNotImplemented()
        };

        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
SELECT id, ticks_utc AS TicksUtc, process_id AS ProcessID,
event_id AS EventId, event_name AS EventName
FROM public.app_events",
            _ => ThrowSqlNotImplemented()
        };

        public string Delete => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.app_events",
            _ => ThrowSqlNotImplemented()
        };
    }
}
