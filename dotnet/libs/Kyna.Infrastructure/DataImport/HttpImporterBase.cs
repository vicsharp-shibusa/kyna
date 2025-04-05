using Kyna.Common;
using Kyna.Infrastructure.Database;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Text.Json;

namespace Kyna.Infrastructure.DataImport;

internal abstract class HttpImporterBase : IDisposable
{
    protected Guid? _processId;
    protected readonly string? _apiKey;

    protected readonly ApiTransactionService _transactionService;
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _serializerOptions = JsonSerializerOptionsRepository.Custom;

    protected readonly ConcurrentBag<(string Uri, string Category, string? SubCategory)> _concurrentBag;

    protected readonly DbDef _dbDef;

    protected HttpImporterBase(DbDef dbDef, string baseUri, string? apiKey = null, Guid? processId = null)
    {
        ArgumentNullException.ThrowIfNull(dbDef);
        ArgumentNullException.ThrowIfNull(baseUri);

        _dbDef = dbDef;
        _processId = processId;
        _transactionService = new ApiTransactionService(dbDef);
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(baseUri),
        };
        _apiKey = apiKey;
        _concurrentBag = [];
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    public abstract string Source { get; }

    protected virtual async Task InvokeApiCallAsync(string uri, string category, string? subCategory = null,
        bool letItFail = false,
        CancellationToken cancellationToken = default)
    {
        _ = await GetStringResponseAsync(uri, category, subCategory, letItFail, cancellationToken)
            .ConfigureAwait(false);
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
            return await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        /*
         * Prevent secrets from leaking by hiding them before preserving the transaction.
         */
        var t = _transactionService.RecordTransactionAsync("GET", HideToken(uri), Source, category, response,
            _httpClient.DefaultRequestHeaders, payload: null, subCategory, processId: _processId)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            if (letItFail)
            {
                await t;
                throw new Exception($"Import failed with API response failure: {response.StatusCode}");
            }

            _concurrentBag.Add((uri, category, subCategory));
            await t;
            return "";
        }

        await t;
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// This protects the api key from leaking via error messages.
    /// </summary>
    /// <param name="text">Text with api key.</param>
    /// <returns></returns>
    protected virtual string HideToken(string text) => _apiKey == null ? text : text.Replace(_apiKey, "{SECRET_KEY}");


    // see also: https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry?tab=readme-ov-file#new-jitter-recommendation
    private readonly IEnumerable<TimeSpan> _delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(3),
        retryCount: 10);

    private AsyncRetryPolicy<HttpResponseMessage> RetryPolicy => Policy
        .Handle<HttpRequestException>()
        .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests ||
                                            r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .WaitAndRetryAsync(_delay);


}
