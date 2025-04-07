namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetMigrationsSql()
    {
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteLeadingPriceGaps, DatabaseEngine.PostgreSql),
            @"
DELETE FROM eod_prices
WHERE (source, code, date_eod) IN (
    SELECT e.source, e.code, e.date_eod
    FROM eod_prices e
    JOIN (
        SELECT code, MAX(date_eod) AS latest_date_eod
        FROM (
            SELECT 
                code,
                date_eod,
                LAG(date_eod) OVER (PARTITION BY code ORDER BY date_eod ASC) AS prev_date_eod,
                ROW_NUMBER() OVER (PARTITION BY code ORDER BY date_eod ASC) AS row_num
            FROM eod_prices
            WHERE source = @Source AND process_id = @ProcessId
        ) t
        WHERE 
            t.row_num > 1
            AND t.date_eod >= t.prev_date_eod + INTERVAL '30 days'
        GROUP BY code
    ) latest
    ON e.code = latest.code
    WHERE e.date_eod < latest.latest_date_eod
    AND e.source = @Source
    AND e.process_id = @ProcessId
);");
    }
}