namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetStatsSql()
    {
        // Upsert a stats_build record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertStatsBuild, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.stats_build(
    id, source, config_content, created_at, updated_at, created_at_unix_ms, updated_at_unix_ms,
    process_id)
VALUES (
    @Id, @Source, @ConfigContent, @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs, @ProcessId
)
ON CONFLICT (id) DO UPDATE SET
    source = EXCLUDED.source,
    config_content = EXCLUDED.config_content,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms"
        );

        // Fetch all stats_build records
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectStatsBuild, DatabaseEngine.PostgreSql),
            @"
SELECT
    id AS Id, source AS Source, config_content AS ConfigContent,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    process_id AS ProcessId
FROM public.stats_build"
        );

        // Upsert a stats_details record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertStatsDetail, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.stats_details(
    stats_build_id, code, stat_type, stat_key, stat_val, stat_meta,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms, process_id)
VALUES (
    @StatsBuildId, @Code, @StatType, @StatKey, @StatVal, @StatMeta,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs, @ProcessId
)
ON CONFLICT (stats_build_id, code, stat_type, stat_key) DO UPDATE SET
    stat_val = EXCLUDED.stat_val,
    stat_meta = EXCLUDED.stat_meta,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms,
    process_id = EXCLUDED.process_id"
        );

        // Fetch all stats_details records
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectStatsDetail, DatabaseEngine.PostgreSql),
            @"
SELECT
    stats_build_id AS StatsBuildId, code AS Code, stat_type AS StatType,
    stat_key AS StatKey, stat_val AS StatVal, stat_meta AS StatMeta,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    process_id AS ProcessId
FROM public.stats_details"
        );

        // Upsert a stats record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertStat, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.stats(
    stats_build_id, category, sub_category, stat_type, stat_key, stat_val,
    search_size, sample_size, confidence_lower, confidence_upper,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms, process_id)
VALUES (
    @StatsBuildId, @Category, @SubCategory, @StatType, @StatKey, @StatVal,
    @SearchSize, @SampleSize, @ConfidenceLower, @ConfidenceUpper,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs, @ProcessId
)
ON CONFLICT (stats_build_id, category, sub_category, stat_type, stat_key) DO UPDATE SET
    stat_val = EXCLUDED.stat_val,
    search_size = EXCLUDED.search_size,
    sample_size = EXCLUDED.sample_size,
    confidence_lower = EXCLUDED.confidence_lower,
    confidence_upper = EXCLUDED.confidence_upper,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms,
    process_id = EXCLUDED.process_id"
        );

        // Fetch all stats records
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectStat, DatabaseEngine.PostgreSql),
            @"
SELECT
    stats_build_id AS StatsBuildId, category AS Category, sub_category AS SubCategory,
    stat_type AS StatType, stat_key AS StatKey, stat_val AS StatVal,
    search_size AS SearchSize, sample_size AS SampleSize,
    confidence_lower AS ConfidenceLower, confidence_upper AS ConfidenceUpper,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    process_id AS ProcessId
FROM public.stats"
        );
    }
}