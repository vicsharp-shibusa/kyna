namespace Kyna.Infrastructure.Database;

internal static partial class SqlRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetRemoteFileSql()
    {
        // Upsert a remote file record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertRemoteFile, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.remote_files (
    id, source, provider, location, source_name, local_name, update_date, size, hash_code, process_id,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms)
VALUES (
    @Id, @Source, @Provider, @Location, @SourceName, @LocalName, @UpdateDate, @Size, @HashCode, @ProcessId,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs)
ON CONFLICT (id) DO UPDATE
SET 
    update_date = EXCLUDED.update_date,
    size = EXCLUDED.size,
    hash_code = EXCLUDED.hash_code,
    process_id = EXCLUDED.process_id,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms,
    migrated_at = NULL,
    migrated_at_unix_ms = NULL");

        // Fetch all remote files
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchRemoteFiles, DatabaseEngine.PostgreSql),
            @"
SELECT 
    id, source, provider, location, source_name AS SourceName, local_name AS LocalName,
    update_date AS UpdateDate,
    size,
    hash_code AS HashCode,
    process_id AS ProcessId,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    migrated_at_unix_ms AS MigratedAtUnixMs
FROM public.remote_files");

        // Delete remote files for a specific source
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteRemoteFilesForSource, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.remote_files 
WHERE source = @Source");

        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.MarkRemoteFileAsMigrated, DatabaseEngine.PostgreSql),
            @"
UPDATE public.remote_files
SET
updated_at = @Timestamp,
updated_at_unix_ms = @TimestampMs,
migrated_at = @Timestamp,
migrated_at_unix_ms = @TimestampMs
WHERE id = @Id");

    }
}