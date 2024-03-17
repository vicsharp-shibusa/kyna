namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    internal class ApiTransactionsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Insert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
INSERT INTO public.api_transactions(
ticks_utc, source, category, sub_category, 
request_uri, request_method, request_payload, request_headers, 
response_headers, response_status_code, response_body, process_id)
VALUES (@TicksUtc, @Source, @Category, @SubCategory,
@RequestUri, @RequestMethod, @RequestPayload, @RequestHeaders,
@ResponseHeaders, @ResponseStatusCode, @ResponseBody, @ProcessId)",
            _ => ThrowSqlNotImplemented()
        };
        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT
ticks_utc AS TicksUtc, 
source,
category, 
sub_category AS SubCategory,
request_uri AS RequestUri, 
request_method AS RequestMethod,
request_payload AS RequestPayload,
request_headers AS RequestHeaders,
response_headers AS ResponseHeaders,
response_status_code AS ResponseStatusCode,
response_body AS ResponseBody,
process_id AS ProcessId
FROM public.api_transactions",
            _ => ThrowSqlNotImplemented()
        };
        public string FetchResponseBodyForId => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT response_body AS ResponseBody
FROM public.api_transactions WHERE id = @Id",
            _ => ThrowSqlNotImplemented()
        };
        public string DeleteForSource => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.api_transactions WHERE source = @Source",
            _ => ThrowSqlNotImplemented()
        };
        public string Delete => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => $@"DELETE FROM public.api_transactions",
            _ => ThrowSqlNotImplemented()
        };
        public string FetchForMigration => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT
id,
source,
category, 
sub_category AS SubCategory,
response_status_code AS ResponseStatusCode,
process_id AS ProcessId
FROM public.api_transactions",
            _ => ThrowSqlNotImplemented()
        };
    }
}