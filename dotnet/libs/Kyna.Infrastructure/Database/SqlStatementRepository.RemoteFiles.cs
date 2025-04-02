namespace Kyna.Infrastructure.Database;

internal static partial class SqlStatementRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetRemoteFileSql()
    {
        // Upsert a remote file record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertRemoteFile, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.remote_files (
    source, provider, location, name, update_date, size, hash_code, process_id,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms)
VALUES (
    @Source, @Provider, @Location, @Name, @UpdateDate, @Size, @HashCode, @ProcessId,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs)
ON CONFLICT (source, provider, location, name) DO UPDATE
SET 
    update_date = EXCLUDED.update_date,
    size = EXCLUDED.size,
    hash_code = EXCLUDED.hash_code,
    process_id = EXCLUDED.process_id,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms");

        // Fetch all remote files
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchRemoteFiles, DatabaseEngine.PostgreSql),
            @"
SELECT 
    source, provider, location, name, update_date AS UpdateDate, size, hash_code AS HashCode,
    process_id AS ProcessId,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs
FROM public.remote_files");

        // Delete remote files for a specific source
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteRemoteFilesForSource, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.remote_files 
WHERE source = @Source");
    }
}