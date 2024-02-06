using Kyna.Infrastructure.Database;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Kyna.Infrastructure.DataImport;

public class ApiTransactionService(DbDef dbDef)
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
        DateTime? transactionTime = null)
    {
        var transDao = new Database.DataAccessObjects.ApiTransaction()
        {
            TicksUtc = transactionTime?.Ticks ?? DateTime.UtcNow.Ticks,
            Source = source,
            Category = category,
            SubCategory = subCategory,
            RequestHeaders = JsonSerializer.Serialize(requestHeaders, _serializerOptions),
            RequestMethod = method,
            RequestPayload = payload is null ? null : await payload.ReadAsStringAsync(),
            RequestUri = uri,
            ResponseStatusCode = ((int)response.StatusCode).ToString(),
            ResponseHeaders = JsonSerializer.Serialize(response.Headers, _serializerOptions),
            ResponseBody = await response.Content.ReadAsStringAsync()
        };

        await _dbContext.ExecuteAsync(_dbContext.Sql.ApiTransactions.Insert, transDao);
    }
}
