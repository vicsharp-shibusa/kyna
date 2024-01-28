namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    public string InsertApiTransaction => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"
INSERT INTO public.api_transactions(
 timestamp_utc, source, category, sub_category, 
 request_uri, request_method, request_payload, request_headers, 
 response_headers, response_status_code, response_body)
VALUES (@TimestampUtc, @Source, @Category, @SubCategory,
 @RequestUri, @RequestMethod, @RequestPayload, @RequestHeaders,
 @ResponseHeaders, @ResponseStatusCode, @ResponseBody)",
        _ => ThrowSqlNotImplemented()
    };

    public string FetchApiTransaction => _dbDef.Engine switch
    {
        DatabaseEngine.PostgreSql => @"SELECT
timestamp_utc AS TimestampUtc, 
source,
category, 
sub_category AS SubCategory,
request_uri AS RequestUri, 
request_method AS RequestMethod,
request_payload AS RequestPayload,
request_headers AS RequestHeaders,
response_headers AS ResponseHeaders,
response_status_code AS ResponseStatusCode,
response_body AS ResponseBody
FROM public.api_transactions",
        _ => ThrowSqlNotImplemented()
    };
}
