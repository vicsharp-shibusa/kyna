using Kyna.Infrastructure.Database;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Kyna.ApplicationServices.DataImport;

public class ApiTransactionService
{
    private readonly IDbContext _dbContext;
    private readonly SqlRepository _sqlRepository;
    private readonly JsonSerializerOptions _serializerOptions = JsonSerializerOptions.Default;
    
    public ApiTransactionService(DbDef dbDef)
    {
        _dbContext = DbContextFactory.Create(dbDef);
        _sqlRepository = new SqlRepository(dbDef);
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
        DateTime? transactionTime = null)
    {
        var transDao = new Infrastructure.Database.DataAccessObjects.ApiTransaction()
        {
            TimestampUtc = transactionTime ?? DateTime.UtcNow,
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

        await _dbContext.ExecuteAsync(_sqlRepository.InsertApiTransaction, transDao);
    }
}
