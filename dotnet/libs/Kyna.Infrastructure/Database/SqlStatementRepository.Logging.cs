namespace Kyna.Infrastructure.Database;

internal static partial class SqlStatementRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetLoggingSql()
    {
        // Insert a log entry
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.InsertLog, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.logs (
    created_at, process_id, log_level, message, exception, scope)
VALUES (
    @CreatedAt, @ProcessId, @LogLevel, @Message, @Exception, @Scope)");

        // Fetch all logs
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchLogs, DatabaseEngine.PostgreSql),
            @"
SELECT
    id, created_at AS CreatedAt, process_id AS ProcessId,
    log_level AS LogLevel, message, exception, scope
FROM public.logs");

        // Delete all logs (use with caution)
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteLogs, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.logs");

        // Insert an app event
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.InsertAppEvent, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.app_events (
    created_at, process_id, event_id, event_name)
VALUES (
    @CreatedAt, @ProcessId, @EventId, @EventName)");

        // Fetch all app events
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchAppEvents, DatabaseEngine.PostgreSql),
            @"
SELECT
    id, created_at AS CreatedAt, process_id AS ProcessId,
    event_id AS EventId, event_name AS EventName
FROM public.app_events");

        // Delete all app events (use with caution)
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteAppEvents, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.app_events");
    }
}