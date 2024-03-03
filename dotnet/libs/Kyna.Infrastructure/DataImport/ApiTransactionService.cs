using Kyna.Infrastructure.Database;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Kyna.Infrastructure.DataImport;

internal sealed class ApiTransactionService(DbDef dbDef)
{
    private readonly IDbContext _dbContext = DbContextFactory.Create(dbDef);
    private readonly JsonSerializerOptions _serializerOptions = JsonSerializerOptions.Default;

    public async Task RecordTransactionAsync(
        string method,
        string uri,
        string source,
        string category,
        HttpResponseMessage response,
        HttpRequestHeaders requestHeaders,
        HttpContent? payload = null,
        string? subCategory = null,
        DateTime? transactionTime = null,
        Guid? processId = null)
    {
        var transDao = new Database.DataAccessObjects.ApiTransaction()
        {
            TicksUtc = transactionTime?.Ticks ?? DateTime.UtcNow.Ticks,
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

        await _dbContext.ExecuteAsync(_dbContext.Sql.ApiTransactions.Insert, transDao)
            .ConfigureAwait(false);
    }

    public async Task DeleteTransactionsAsync(string source, string category, IEnumerable<string> subCategories)
    {
        string sql = $"DELETE FROM api_transactions WHERE source = @Source AND category = @Category AND sub_category {_dbContext.Sql.GetInCollectionSql("SubCategories")}";

        foreach (var c in subCategories.Select(x => x.Trim()).Chunk(500))
        {
            await _dbContext.ExecuteAsync(sql, new
            {
                source,
                category,
                SubCategories = c
            }).ConfigureAwait(false);
        }
    }
}
