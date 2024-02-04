using Kyna.Common;
using Kyna.Infrastructure.Database;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Kyna.Infrastructure.DataImport;

public abstract class DataImporterBase : IDisposable
{
    protected bool _disposedValue;

    protected readonly string? _apiKey;

    protected readonly ApiTransactionService _transactionService;
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _serializerOptions = JsonOptionsRepository.DefaultSerializerOptions;

    protected readonly ConcurrentBag<(string Uri, string Category, string? SubCategory)> _concurrentBag;

    protected DataImporterBase(DbDef dbDef, string baseUri, string? apiKey = null)
    {
        _transactionService = new ApiTransactionService(dbDef);
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(baseUri),
        };
        _apiKey = apiKey;
        _concurrentBag = [];
    }

    public abstract string Source { get; }

    // see also: https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry?tab=readme-ov-file#new-jitter-recommendation
    private readonly IEnumerable<TimeSpan> _delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(3),
        retryCount: 10);
    
    private AsyncRetryPolicy<HttpResponseMessage> RetryPolicy => Policy
        .Handle<HttpRequestException>()
        .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests ||
                                            r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .WaitAndRetryAsync(_delay);

    protected virtual async Task InvokeApiCallAsync(string uri, string category, string? subCategory = null,
        bool letItFail = false,
        CancellationToken cancellationToken = default)
    {
        _ = await GetStringResponseAsync(uri, category, subCategory, letItFail, cancellationToken);
    }

    protected virtual async Task<string> GetStringResponseAsync(string uri, string category, string? subCategory = null,
        bool letItFail = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentNullException(nameof(uri));
        }

        var response = await RetryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.GetAsync(uri, cancellationToken);
        });

        if ((response.StatusCode == HttpStatusCode.TooManyRequests ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable) && !letItFail)
        {
            _concurrentBag.Add((uri, category, subCategory));
            return "";
        }

        string uriWithoutKey = HideToken(uri);
        await _transactionService.RecordTransactionAsync("GET", uriWithoutKey, Source, category, response,
            _httpClient.DefaultRequestHeaders, payload: null, subCategory);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    protected virtual string HideToken(string text) => _apiKey == null ? text : text.Replace(_apiKey, "{SECRET_KEY}");

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
