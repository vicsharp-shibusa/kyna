using Kyna.Infrastructure.Database;
using System.Data;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Kyna.Infrastructure.DataImport;

internal sealed class ApiTransactionService : IDisposable
{
    private readonly JsonSerializerOptions _serializerOptions = JsonSerializerOptions.Default;
    private readonly DbDef _dbDef;
    public ApiTransactionService(DbDef dbDef)
    {
        _dbDef = dbDef;
    }

    public async Task RecordTransactionAsync(
        string method,
        string uri,
        string source,
        string category,
        HttpResponseMessage response,
        HttpRequestHeaders requestHeaders,
        HttpContent? payload = null,
        string? subCategory = null,
        Guid? processId = null)
    {
        var transDao = new Database.DataAccessObjects.ApiTransaction()
        {
            Source = source,
            Category = category,
            SubCategory = subCategory,
            RequestHeaders = JsonSerializer.Serialize(requestHeaders, _serializerOptions),
            RequestMethod = method,
            RequestPayload = payload is null
                ? null : await payload.ReadAsStringAsync().ConfigureAwait(false),
            RequestUri = uri,
            ResponseStatusCode = ((int)response.StatusCode).ToString(),
            ResponseHeaders = JsonSerializer.Serialize(response.Headers, _serializerOptions),
            ResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false),
            ProcessId = processId
        };

        using var conn = _dbDef.GetConnection();
        await conn.ExecuteAsync(_dbDef.Sql.GetSql(SqlKeys.InsertApiTransaction), transDao)
            .ConfigureAwait(false);
    }

    public async Task DeleteTransactionsAsync(string source, string category, IEnumerable<string> subCategories)
    {
        string sql = $"DELETE FROM api_transactions WHERE source = @Source AND category = @Category AND sub_category {SqlCollection.GetSqlSyntaxForInCollection("SubCategories")}";

        foreach (var c in subCategories.Select(x => x.Trim()).Chunk(500))
        {
            using var conn = _dbDef.GetConnection();
            await conn.ExecuteAsync(sql, new
            {
                source,
                category,
                SubCategories = c
            }).ConfigureAwait(false);
            conn.Close();
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
