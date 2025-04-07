namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetApiTransactionSql()
    {
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.InsertApiTransaction, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.api_transactions(
created_at, created_at_unix_ms, source, category, sub_category, 
request_uri, request_method, request_payload, request_headers, 
response_headers, response_status_code, response_body, process_id)
VALUES (@CreatedAt, @CreatedAtUnixMs, @Source, @Category, @SubCategory,
@RequestUri, @RequestMethod, @RequestPayload, @RequestHeaders,
@ResponseHeaders, @ResponseStatusCode, @ResponseBody, @ProcessId)");

        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchApiTransaction, DatabaseEngine.PostgreSql),
    @"SELECT 
created_at_unix_ms AS CreatedAtUnixMs,
source AS Source, 
category AS Category, 
sub_category AS SubCategory, 
request_uri AS RequestUri, 
request_method AS RequestMethod, 
request_payload AS RequestPayload, 
request_headers AS RequestHeaders, 
response_headers AS ResponseHeaders, 
response_status_code AS ResponseStatusCode, 
response_body AS ResponseBody, 
process_id AS ProcessId
FROM public.api_transactions");

        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchApiResponseBodyForId, DatabaseEngine.PostgreSql),
@"SELECT response_body AS ResponseBody FROM public.api_transactions WHERE id = @Id");

        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteApiTransactionsForSource, DatabaseEngine.PostgreSql),
@"DELETE FROM public.api_transactions WHERE source = @Source");

        /*
         * DANGER! DANGER! DANGER! Deletes without WHERE clauses are not considered good practice.
         */
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteApiTransactions, DatabaseEngine.PostgreSql),
@"DELETE FROM public.api_transactions");

        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchApiTransactionsForMigration, DatabaseEngine.PostgreSql),
@"SELECT
id AS Id,
source AS Source,
category AS Category, 
sub_category AS SubCategory,
response_status_code AS ResponseStatusCode,
process_id AS ProcessId
FROM public.api_transactions");
    }
}