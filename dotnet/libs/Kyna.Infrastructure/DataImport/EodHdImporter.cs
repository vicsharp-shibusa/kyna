using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Common.Logging;
using Kyna.EodHistoricalData.Models;
using Kyna.Infrastructure.Database;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataImport;

internal sealed class EodHdImporter : DataImporterBase, IExternalDataImporter
{
    private readonly IDbContext _dbContext;

    private readonly object _locker = new();

    private int _usage = 0;
    private int _available = 100_000;
    private int _dailyLimit = 100_000;
    private string _apiRequestsDate = "";

    private int? _maxParallelization = null;
    private bool _useBulk = false;
    private readonly bool _dryRun = false;

    private readonly ImportAction[] _importActions;

    private readonly ReadOnlyDictionary<string, string[]> _exchanges;
    private readonly ReadOnlyDictionary<string, string[]> _symbolTypes;
    private readonly ReadOnlyDictionary<string, string[]> _options;
    private readonly ReadOnlyDictionary<string, DateOnly[]> _dateRanges;

    public EodHdImporter(DbDef dbDef, string apiKey, Guid? processId = null) : base(dbDef, Constants.Uris.Base, apiKey, processId)
    {
        _dbContext = DbContextFactory.Create(dbDef);
        _importActions = [];
        var dict = new Dictionary<string, string[]>();
        _exchanges = new ReadOnlyDictionary<string, string[]>(dict);
        _symbolTypes = new ReadOnlyDictionary<string, string[]>(dict);
        _options = new ReadOnlyDictionary<string, string[]>(dict);
        _dateRanges = new ReadOnlyDictionary<string, DateOnly[]>(new Dictionary<string, DateOnly[]>());
        _dateRanges = new ReadOnlyDictionary<string, DateOnly[]>(new Dictionary<string, DateOnly[]>());
    }

    public EodHdImporter(DbDef dbDef, DataImportConfiguration importConfig, Guid? processId = null, bool dryRun = false)
        : base(dbDef, Constants.Uris.Base, importConfig.ApiKey, processId)
    {
        if (!importConfig.Source.Equals(Source, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{nameof(EodHdImporter)} can only be called with an import configuration containing a source name of {Source}");
        }

        _dbContext = DbContextFactory.Create(dbDef);

        _importActions = ExtractImportActions(importConfig);

        if (_importActions.Length == 0)
        {
            throw new ArgumentException("No actions specified in the import configuration.");
        }

        _exchanges = DataImportConfiguration.CreateDictionary(importConfig.Exchanges);
        _symbolTypes = DataImportConfiguration.CreateDictionary(importConfig.SymbolTypes);
        _options = DataImportConfiguration.CreateDictionary(importConfig.Options);
        _dateRanges = ConfigureDateRanges(DataImportConfiguration.CreateDictionary(importConfig.DateRanges));

        ConfigureOptions(_options);

        _dryRun = dryRun;
    }

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public const string SourceName = "eodhd.com";

    public override string Source => SourceName;

    public async Task<string> GetInfoAsync()
    {
        StringBuilder sb = new();

        try
        {
            await InvokeUserCallAsync(CancellationToken.None).ConfigureAwait(false);

            sb.AppendLine($"Source        : {SourceName}");
            sb.AppendLine($"Daily Limit   : {_dailyLimit:#,##0}");
            sb.AppendLine($"Used          : {_usage:#,##0}");
            sb.AppendLine($"Available     : {_available:#,##0}");
            sb.AppendLine($"Requests Date : {_apiRequestsDate:yyyy-MM-dd}");
        }
        catch
        {
            sb.Clear();
            sb.AppendLine("An error has occurred.");
            sb.AppendLine("A likely scenario is that your api key is missing or invalid.");
            sb.AppendLine($"Your api key: \"{_apiKey}\"");
        }

        return sb.ToString();
    }

    public (bool IsDangerous, string[] DangerMessages) ContainsDanger()
    {
        var purgeAction = FindImportAction(Constants.Actions.Purge);

        if (!_dryRun &&
            !purgeAction.Equals(ImportAction.Default) &&
            (purgeAction.Details!.Length == 0 ||
            !purgeAction.Details[0].Equals("false", StringComparison.OrdinalIgnoreCase)))
        {
            return (true, ["This configuration file contains a command to purge all import data. Are you sure you want to do this?"]);
        }

        return (false, []);
    }

    public async Task<TimeSpan> ImportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stopwatch timer = Stopwatch.StartNew();

        await PurgeTransactionsForSourceAsync(cancellationToken).ConfigureAwait(false);
        await InvokeUserCallAsync(cancellationToken).ConfigureAwait(false);
        await InvokeExchangeListCallAsync(cancellationToken).ConfigureAwait(false);
        await InvokeExchangeDetailsCallAsync(cancellationToken).ConfigureAwait(false);

        var symbols = await InvokeExchangeSymbolsListCallAsync(cancellationToken).ConfigureAwait(false);

        if (((symbols?.Keys.Count ?? 0) != 0) || _dryRun)
        {
            InvokeImportForSymbols(symbols!, cancellationToken);
        }

        await InvokeCalendarEarningsCallAsync(cancellationToken).ConfigureAwait(false);
        await InvokeCalendarIposCallAsync(cancellationToken).ConfigureAwait(false);
        await InvokeCalendarSplitsCallAsync(cancellationToken).ConfigureAwait(false);

        if (!_concurrentBag.IsEmpty)
        {
            _maxParallelization = 0;
            Communicate?.Invoke(this, new CommunicationEventArgs($"Processing {_concurrentBag.Count} stragglers.", null));

            foreach (var item in _concurrentBag)
            {
                await InvokeApiCallAsync(item.Uri, item.Category, item.SubCategory, 
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        timer.Stop();
        return timer.Elapsed;
    }

    private void InvokeImportForSymbols(IDictionary<string, Symbol[]> symbols, CancellationToken cancellationToken)
    {
        bool runEod = ShouldActionRun(Constants.Actions.EndOfDayPrices);
        bool runSplits = ShouldActionRun(Constants.Actions.Splits);
        bool runDividends = ShouldActionRun(Constants.Actions.Dividends);
        bool runFundamentals = ShouldActionRun(Constants.Actions.Fundamentals);
        bool runInsiderTransactions = ShouldActionRun(Constants.Actions.InsiderTransactions);

        List<Task> tasks = new(5);

        if (_dryRun)
        {
            string prefix = _useBulk ? "Bulk " : "";

            if (runEod)
            {
                CommunicateAction($"{prefix}{Constants.Actions.EndOfDayPrices}");
            }
            if (runSplits)
            {
                CommunicateAction($"{prefix}{Constants.Actions.Splits}");
            }
            if (runDividends)
            {
                CommunicateAction($"{prefix}{Constants.Actions.Dividends}");
            }
            if (runFundamentals)
            {
                CommunicateAction(Constants.Actions.Fundamentals);
            }
            if (runInsiderTransactions)
            {
                CommunicateAction(Constants.Actions.InsiderTransactions);
            }
        }

        foreach (var exchange in symbols.Keys)
        {
            var filteredSymbols = FilterSymbolsForExchange(exchange, symbols[exchange]);
            filteredSymbols = FilterSymbolsForType(exchange, filteredSymbols);

            if (filteredSymbols.Length == 0)
            {
                Communicate?.Invoke(this, new CommunicationEventArgs("No symbols survived the filtering", nameof(InvokeImportForSymbols)));
                continue;
            }

            if (_useBulk)
            {
                if (runEod)
                {
                    tasks.Add(InvokeBulkEodCallForExchangeAsync(exchange, cancellationToken));
                }

                if (runSplits)
                {
                    tasks.Add(InvokeBulkSplitsCallForExchangeAsync(exchange, cancellationToken));
                }

                if (runDividends)
                {
                    tasks.Add(InvokeBulkDividendsCallForExchangeAsync(exchange, cancellationToken));
                }
            }
            else
            {
                if (runEod)
                {
                    tasks.Add(InvokeEodCallsForExchangeAsync(exchange, filteredSymbols, cancellationToken));
                }

                if (runSplits)
                {
                    tasks.Add(InvokeSplitsCallForExchangeAsync(exchange, filteredSymbols, cancellationToken));
                }

                if (runDividends)
                {
                    tasks.Add(InvokeDividendsCallForExchangeAsync(exchange, filteredSymbols, cancellationToken));
                }
            }

            if (runFundamentals)
            {
                tasks.Add(InvokeFundamentalsCallAsync(exchange, filteredSymbols, cancellationToken));
            }

            if (runInsiderTransactions)
            {
                tasks.Add(InvokeInsiderTransactionsCallAsync(exchange, filteredSymbols, cancellationToken));
            }

            Task.WaitAll([.. tasks], cancellationToken);
        }
    }

    private Task PurgeTransactionsForSourceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var purgeAction = FindImportAction(Constants.Actions.Purge);

        if (!purgeAction.Equals(ImportAction.Default) &&
            (purgeAction.Details!.Length == 0 ||
            !purgeAction.Details[0].Equals("false", StringComparison.OrdinalIgnoreCase)))
        {
            if (!_dryRun)
            {
                CommunicateAction(Constants.Actions.Purge);

                return _dbContext.ExecuteAsync(_dbContext.Sql.ApiTransactions.DeleteForSource, new { Source },
                    cancellationToken: cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    private async Task InvokeUserCallAsync(CancellationToken cancellationToken, bool alreadyLocked = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var uri = BuildUserUri();

        CommunicateAction(Constants.Actions.User);

        var response = await GetStringResponseAsync(uri, Constants.Actions.User,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var user = JsonSerializer.Deserialize<User>(response, JsonOptionsRepository.DefaultSerializerOptions);

        _apiRequestsDate = user.ApiRequestsDate.ToString("yyyy-MM-dd");

        if (DateOnly.TryParse(_apiRequestsDate, out DateOnly date))
        {
            _dailyLimit = user.DailyRateLimit;
            var apiRequests = DateOnly.FromDateTime(DateTime.UtcNow).Equals(date)
                ? user.ApiRequests : 0;

            if (alreadyLocked)
            {
                _usage = apiRequests;
                _available = _dailyLimit - _usage;
            }
            else
            {
                lock (_locker)
                {
                    _usage = apiRequests;
                    _available = _dailyLimit - _usage;
                }
            }
        }
        else
        {
            throw new Exception($"Could not parse {_apiRequestsDate}");
        }
    }

    private async Task InvokeExchangeListCallAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exchangeListAction = FindImportAction(Constants.Actions.ExchangeList);

        if (!exchangeListAction.Equals(ImportAction.Default) &&
            (exchangeListAction.Details!.Length == 0 ||
            !exchangeListAction.Details[0].Equals("false", StringComparison.OrdinalIgnoreCase)))
        {
            var uri = BuildExchangeListUri();
            var ep = FindEndPointForUri(uri);
            CheckApiLimit(cancellationToken, ep.Cost);

            CommunicateAction(Constants.Actions.ExchangeList);

            if (!_dryRun)
            {
                await InvokeApiCallAsync(uri, Constants.Actions.ExchangeList, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                AddCallToUsage(uri);
            }
        }
    }

    private async Task InvokeExchangeDetailsCallAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exchangeDetailsAction = FindImportAction(Constants.Actions.ExchangeDetails);

        if (!exchangeDetailsAction.Equals(ImportAction.Default) &&
            (exchangeDetailsAction.Details?.Length ?? 0) > 0)
        {
            foreach (var exchange in exchangeDetailsAction.Details!.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                CommunicateAction($"{Constants.Actions.ExchangeDetails} for {exchange}");

                var uri = BuildExchangeDetailsUri(exchange);

                var ep = FindEndPointForUri(uri);
                CheckApiLimit(cancellationToken, ep.Cost);

                if (!_dryRun)
                {
                    await InvokeApiCallAsync(uri, Constants.Actions.ExchangeDetails, exchange,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    AddCallToUsage(uri);
                }
            }
        }
    }

    private async Task<IDictionary<string, Symbol[]>> InvokeExchangeSymbolsListCallAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, Symbol[]> symbols = [];

        var exchangeSymbolsListAction = FindImportAction(Constants.Actions.ExchangeSymbolList);

        if (!exchangeSymbolsListAction.Equals(ImportAction.Default) &&
            (exchangeSymbolsListAction.Details?.Length ?? 0) > 0)
        {
            foreach (var exchange in exchangeSymbolsListAction.Details!.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                CommunicateAction($"{Constants.Actions.ExchangeSymbolList} for {exchange}");
                var uri = BuildSymbolListUri(exchange);

                var ep = FindEndPointForUri(uri);
                CheckApiLimit(cancellationToken, ep.Cost);

                if (!_dryRun)
                {
                    string json = await GetStringResponseAsync(uri, Constants.Actions.ExchangeSymbolList,
                        exchange, cancellationToken: cancellationToken).ConfigureAwait(false);

                    symbols.Add(exchange, JsonSerializer.Deserialize<Symbol[]>(json, JsonOptionsRepository.DefaultSerializerOptions)!);

                    AddCallToUsage(uri);
                }
            }
        }

        return symbols;
    }

    private async Task InvokeEodCallsForExchangeAsync(string exchange, Symbol[] symbols, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (symbols.Length != 0)
        {
            var uri = BuildEodUri(exchange);
            var ep = FindEndPointForUri(uri);

            symbols = FilterSymbolsForCost(ep, symbols);

            if (_maxParallelization.GetValueOrDefault() > 1)
            {
                await Parallel.ForEachAsync(symbols, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = _maxParallelization.GetValueOrDefault()
                }, async (symbol, ct) =>
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.EndOfDayPrices} for {code}");

                    uri = BuildEodUri($"{symbol.Code}.{exchange}");
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.EndOfDayPrices, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var symbol in symbols)
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.EndOfDayPrices} for {code}");

                    uri = BuildEodUri($"{symbol.Code}.{exchange}");
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.EndOfDayPrices, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }
            }
        }
    }

    private async Task InvokeSplitsCallForExchangeAsync(string exchange, Symbol[] symbols, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (symbols.Length != 0)
        {
            var uri = BuildSplitsUri(exchange);
            var ep = FindEndPointForUri(uri);

            symbols = FilterSymbolsForCost(ep, symbols);

            if (_maxParallelization.GetValueOrDefault() > 0)
            {
                await Parallel.ForEachAsync(symbols, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = _maxParallelization.GetValueOrDefault()
                }, async (symbol, ct) =>
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.Splits} for {code}");

                    uri = BuildSplitsUri($"{symbol.Code}.{exchange}");
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.Splits, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var symbol in symbols)
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.Splits} for {code}");

                    uri = BuildSplitsUri($"{symbol.Code}.{exchange}");
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.Splits, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }
            }
        }
    }

    private async Task InvokeDividendsCallForExchangeAsync(string exchange, Symbol[] symbols, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (symbols.Length != 0)
        {
            var uri = BuildDividendsUri(exchange);
            var ep = FindEndPointForUri(uri);

            symbols = FilterSymbolsForCost(ep, symbols);

            if (_maxParallelization.GetValueOrDefault() > 0)
            {
                await Parallel.ForEachAsync(symbols, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = _maxParallelization.GetValueOrDefault()
                }, async (symbol, ct) =>
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.Dividends} for {code}");
                    uri = BuildDividendsUri(code);
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.Dividends, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var symbol in symbols)
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.Dividends} for {code}");

                    uri = BuildDividendsUri(code);
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.Dividends, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }
            }
        }
    }

    private Task InvokeBulkEodCallForExchangeAsync(string exchange, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CommunicateAction($"{Constants.Actions.BulkEndOfDayPrices} for {exchange}");

        var uri = BuildBulkPriceActionUri(exchange);
        var ep = FindEndPointForUri(uri);
        CheckApiLimit(cancellationToken, ep.Cost);

        AddCallToUsage(uri);

        if (_dryRun)
        {
            return Task.CompletedTask;
        }
        return InvokeApiCallAsync(uri, Constants.Actions.BulkEndOfDayPrices, cancellationToken: cancellationToken);
    }

    private Task InvokeBulkSplitsCallForExchangeAsync(string exchange, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CommunicateAction($"{Constants.Actions.BulkSplits} for {exchange}");

        var uri = BuildBulkSplitsUri(exchange);
        var ep = FindEndPointForUri(uri);
        CheckApiLimit(cancellationToken, ep.Cost);

        AddCallToUsage(uri);

        if (_dryRun)
        {
            return Task.CompletedTask;
        }

        return InvokeApiCallAsync(uri, Constants.Actions.BulkSplits, cancellationToken: cancellationToken);
    }

    private Task InvokeBulkDividendsCallForExchangeAsync(string exchange, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CommunicateAction($"{Constants.Actions.BulkDividends} for {exchange}");

        var uri = BuildBulkDividendsUri(exchange);
        var ep = FindEndPointForUri(uri);
        CheckApiLimit(cancellationToken, ep.Cost);

        AddCallToUsage(uri);

        if (_dryRun)
        {
            return Task.CompletedTask;
        }

        return InvokeApiCallAsync(uri, Constants.Actions.BulkDividends, cancellationToken: cancellationToken);
    }

    private async Task InvokeInsiderTransactionsCallAsync(string exchange, Symbol[] symbols, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (symbols.Length != 0)
        {
            var uri = BuildInsiderTransactionsUri(exchange);
            var ep = FindEndPointForUri(uri);

            symbols = FilterSymbolsForCost(ep, symbols);

            if (_maxParallelization.GetValueOrDefault() > 0)
            {
                await Parallel.ForEachAsync(symbols, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = _maxParallelization.GetValueOrDefault()
                }, async (symbol, ct) =>
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.InsiderTransactions} for {code}");
                    uri = BuildInsiderTransactionsUri(code);
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.InsiderTransactions, code,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var symbol in symbols)
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.InsiderTransactions} for {code}");

                    uri = BuildInsiderTransactionsUri(code);
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.InsiderTransactions,
                            code, cancellationToken: cancellationToken).ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }
            }
        }

    }

    private async Task InvokeCalendarEarningsCallAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calendarEarningsAction = FindImportAction(Constants.Actions.CalendarEarnings);

        if (!calendarEarningsAction.Equals(ImportAction.Default) && calendarEarningsAction.Details!.Length == 0)
        {
            var uri = BuildCalendarEarningsUri();
            var ep = FindEndPointForUri(uri);
            CheckApiLimit(cancellationToken, ep.Cost);

            CommunicateAction($"{Constants.Actions.CalendarEarnings}");

            if (!_dryRun)
            {
                await InvokeApiCallAsync(uri, Constants.Actions.CalendarEarnings, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                AddCallToUsage(uri);
            }
        }
    }

    private async Task InvokeCalendarIposCallAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calendarIposAction = FindImportAction(Constants.Actions.CalendarIpos);

        if (!calendarIposAction.Equals(ImportAction.Default) && calendarIposAction.Details!.Length == 0)
        {
            var uri = BuildCalendarIposUri();
            var ep = FindEndPointForUri(uri);
            CheckApiLimit(cancellationToken, ep.Cost);

            CommunicateAction($"{Constants.Actions.CalendarIpos}");

            if (!_dryRun)
            {
                await InvokeApiCallAsync(uri, Constants.Actions.CalendarIpos, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                AddCallToUsage(uri);
            }
        }
    }

    private async Task InvokeCalendarSplitsCallAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calendarSplitsAction = FindImportAction(Constants.Actions.CalendarSplits);

        if (!calendarSplitsAction.Equals(ImportAction.Default) && calendarSplitsAction.Details!.Length == 0)
        {
            var uri = BuildCalendarSplitsUri();
            var ep = FindEndPointForUri(uri);
            CheckApiLimit(cancellationToken, ep.Cost);

            CommunicateAction($"{Constants.Actions.CalendarSplits}");

            if (!_dryRun)
            {
                await InvokeApiCallAsync(uri, Constants.Actions.CalendarSplits, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                AddCallToUsage(uri);
            }
        }
    }

    private async Task InvokeFundamentalsCallAsync(string exchange, Symbol[] symbols, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (symbols.Length != 0)
        {
            var uri = BuildFundamentalsUri(exchange);
            var ep = FindEndPointForUri(uri);

            symbols = FilterSymbolsForCost(ep, symbols);

            if (_maxParallelization.GetValueOrDefault() > 0)
            {
                await Parallel.ForEachAsync(symbols, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = _maxParallelization.GetValueOrDefault()
                }, async (symbol, ct) =>
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.Fundamentals} for {code}");

                    uri = BuildFundamentalsUri($"{symbol.Code}.{exchange}");
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.Fundamentals, code, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var symbol in symbols)
                {
                    string code = $"{symbol.Code}.{exchange}";

                    CommunicateAction($"{Constants.Actions.Fundamentals} for {code}");

                    uri = BuildFundamentalsUri($"{symbol.Code}.{exchange}");
                    ep = FindEndPointForUri(uri);
                    CheckApiLimit(cancellationToken, ep.Cost);

                    if (!_dryRun)
                    {
                        await InvokeApiCallAsync(uri, Constants.Actions.Fundamentals, code, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                        AddCallToUsage(uri);
                    }
                }
            }
        }
    }

    private void CommunicateAction(string message)
    {
        message = _dryRun ? $"{message} (dry run)" : message;

        Communicate?.Invoke(this, new CommunicationEventArgs(message, nameof(EodHdImporter)));
    }

    private static ImportAction[] ExtractImportActions(DataImportConfiguration importConfig)
    {
        List<ImportAction> actions = new(importConfig.Actions.Keys.Count);
        foreach (var kvp in importConfig.Actions)
        {
            var action = kvp.Key;

            if (!Constants.Actions.ValueExists(kvp.Key))
            {
                KLogger.LogWarning($"Attempted to instantiate {nameof(EodHdImporter)} with an invalid action of {kvp.Key}.");
                continue;
            }

            string val = kvp.Value;
            string[] vals = string.IsNullOrWhiteSpace(val) ? []
                : val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            actions.Add(new(action, vals));
        }

        return [.. actions];
    }

    private static (DateOnly? From, DateOnly? To) ExtractDateRange(IDictionary<string, string[]> options, string key)
    {
        if (!options.Any())
        {
            return (null, null);
        }

        DateOnly? from = null, to = null;

        if (options.TryGetValue(key, out string[]? value) && value.Length > 0)
        {
            foreach (var item in value)
            {
                if (DateOnly.TryParse(item, out DateOnly dt))
                {
                    if (from == null)
                    {
                        from = dt;
                    }
                    else if (to == null)
                    {
                        if (dt < from)
                        {
                            to = from;
                            from = dt;
                        }
                        else
                        {
                            to = dt;
                        }
                    }
                    else
                    {
                        if (dt < from)
                        {
                            from = dt;
                        }
                        else if (dt > to)
                        {
                            to = dt;
                        }
                    }
                }
            }
        }

        return (from, to);
    }

    private static ReadOnlyDictionary<string, DateOnly[]> ConfigureDateRanges(IDictionary<string, string[]> dateRanges)
    {
        var ranges = new Dictionary<string, DateOnly[]>(6);

        foreach (var key in new string[] {
            Constants.Actions.Prices,
            Constants.Actions.Dividends,
            Constants.Actions.Splits,
            Constants.Actions.CalendarEarnings,
            Constants.Actions.CalendarIpos,
            Constants.Actions.InsiderTransactions
        })
        {
            var (From, To) = ExtractDateRange(dateRanges, key);
            if (From.HasValue && To.HasValue)
            {
                ranges.Add(key, [From.Value, To.Value]);
            }
            else if (From.HasValue)
            {
                ranges.Add(key, [From.Value]);
            }
        }

        return new ReadOnlyDictionary<string, DateOnly[]>(ranges);
    }

    private void ConfigureOptions(ReadOnlyDictionary<string, string[]> options)
    {
        if (options.TryGetValue(Constants.OptionKeys.MaxParallelization, out string[]? value) && value.Length != 0)
        {
            if (int.TryParse(value[0], out int maxP))
            {
                _maxParallelization = maxP;
            }
        }

        if (options.TryGetValue(Constants.OptionKeys.Bulk, out value) && value.Length != 0)
        {
            _useBulk = !value[0].Equals("false", StringComparison.OrdinalIgnoreCase);
        }
    }

    private Symbol[] FilterSymbolsForExchange(string exchange, Symbol[] symbols)
    {
        if (_exchanges.Count == 0)
        {
            return symbols;
        }

        if (_exchanges.TryGetValue(exchange, out string[]? value))
        {
            return symbols.Where(s => value.Contains(s.Exchange)).ToArray();
        }

        return [];
    }

    private Symbol[] FilterSymbolsForType(string exchange, Symbol[] symbols)
    {
        if (_symbolTypes.Count == 0)
        {
            return symbols;
        }

        if (_symbolTypes.TryGetValue(exchange, out string[]? value))
        {
            return symbols.Where(s => value.Contains(s.Type)).ToArray();
        }

        return [];
    }

    private Symbol[] FilterSymbolsForCost(EndPoint endPoint, Symbol[] symbols, [CallerMemberName] string caller = "")
    {
        int totalCost = endPoint.Cost.GetValueOrDefault() * symbols.Length;

        lock (_locker)
        {
            if (totalCost > _available)
            {
                if ((endPoint.Cost ?? 0) <= 0)
                {
                    endPoint.Cost = 1;
                }

                int numToTake = _available / endPoint.Cost.GetValueOrDefault();

                Communicate?.Invoke(this,
                    new CommunicationEventArgs(
                        $"Reducing number of symbols for {caller} from {symbols.Length} to {numToTake}.",
                        nameof(EodHdImporter)));

                return symbols.Take(numToTake).ToArray();
            }
        }

        return symbols;
    }

    private void CheckApiLimit(CancellationToken cancellationToken, int? expectedCost = null, [CallerMemberName] string caller = "")
    {
        lock (_locker)
        {
            // double check
            if ((expectedCost == null && _available < 1) || (expectedCost != null && _available < expectedCost))
            {
                InvokeUserCallAsync(cancellationToken, true).GetAwaiter().GetResult();
            }
            if (expectedCost == null && _available < 1)
            {
                throw new ApiLimitReachedException(caller);
            }

            if (expectedCost != null && _available < expectedCost)
            {
                throw new ApiLimitReachedException($"Available credits ({_available}) is less than expected cost of {expectedCost} ({caller})");
            }
        }
    }

    private string GetTokenAndFormat(string format = "json") => $"{GetToken()}&{GetFormat(format)}";

    private string GetToken() => $"api_token={_apiKey}";

    private static string GetFormat(string format = "json") => $"fmt={format}";

    private static string BuildFromAndTo(DateOnly[] dates)
    {
        StringBuilder sb = new();

        if (dates.Length > 0)
        {
            sb.Append($"from={dates[0]:yyyy-MM-dd}");
        }
        if (dates.Length > 1)
        {
            sb.Append($"&to={dates[1]:yyyy-MM-dd}");
        }

        return sb.ToString();
    }

    private string BuildUserUri() => $"{Constants.Uris.User}?{GetTokenAndFormat()}";

    private string BuildExchangeListUri() => $"{Constants.Uris.ExchangeList}?{GetTokenAndFormat()}";

    private string BuildExchangeDetailsUri(string exchangeCode) =>
        $"{Constants.Uris.ExchangeDetails}{exchangeCode}?{GetTokenAndFormat()}";

    private string BuildSymbolListUri(string exchangeCode) =>
        $"{Constants.Uris.ExchangeSymbolList}{exchangeCode}?{GetTokenAndFormat()}";

    private string BuildEodUri(string symbol)
    {
        StringBuilder uri = new($"{Constants.Uris.Eod}{symbol.ToUpper()}?{GetTokenAndFormat()}&period=d&order=a");

        if (_dateRanges.TryGetValue(Constants.Actions.EndOfDayPrices, out DateOnly[]? value))
        {
            string fromAndTo = BuildFromAndTo(value);
            if (!string.IsNullOrWhiteSpace(fromAndTo))
            {
                uri.Append($"&{fromAndTo}");
            }
        }

        return uri.ToString();
    }

    private string BuildSplitsUri(string symbol)
    {
        StringBuilder uri = new($"{Constants.Uris.Splits}{symbol.ToUpper()}?{GetTokenAndFormat()}");

        if (_dateRanges.TryGetValue(Constants.Actions.Splits, out DateOnly[]? value))
        {
            string fromAndTo = BuildFromAndTo(value);
            if (!string.IsNullOrWhiteSpace(fromAndTo))
            {
                uri.Append($"&{fromAndTo}");
            }
        }

        return uri.ToString();
    }

    private string BuildDividendsUri(string symbol)
    {
        StringBuilder uri = new($"{Constants.Uris.Dividends}{symbol.ToUpper()}?{GetTokenAndFormat()}");

        if (_dateRanges.TryGetValue(Constants.Actions.Dividends, out DateOnly[]? value))
        {
            string fromAndTo = BuildFromAndTo(value);
            if (!string.IsNullOrWhiteSpace(fromAndTo))
            {
                uri.Append($"&{fromAndTo}");
            }
        }

        return uri.ToString();
    }

    private string BuildBulkPriceActionUri(string exchangeCode, DateOnly? date = null)
    {
        StringBuilder uri = new($"{Constants.Uris.BulkEod}{exchangeCode.ToUpper()}?{GetTokenAndFormat()}");

        if (date.HasValue)
        {
            uri.Append($"&date={date.Value:yyyy-MM-dd}");
        }

        return uri.ToString();
    }

    private string BuildBulkSplitsUri(string exchangeCode, DateOnly? date = null) =>
        $"{BuildBulkPriceActionUri(exchangeCode, date)}&type=splits";

    private string BuildBulkDividendsUri(string exchangeCode, DateOnly? date = null) =>
        $"{BuildBulkPriceActionUri(exchangeCode, date)}&type=dividends";

    private string BuildFundamentalsUri(string symbol) => $"{Constants.Uris.Fundamentals}{symbol}?{GetTokenAndFormat()}";

    private string BuildCalendarEarningsUri() => $"{Constants.Uris.Calendar}earnings?{GetTokenAndFormat()}";

    private string BuildCalendarIposUri() => $"{Constants.Uris.Calendar}ipos?{GetTokenAndFormat()}";

    private string BuildCalendarSplitsUri() => $"{Constants.Uris.Calendar}splits?{GetTokenAndFormat()}";

    private string BuildInsiderTransactionsUri(string? code = null)
    {
        StringBuilder result = new($"{Constants.Uris.InsiderTransactions}?{GetTokenAndFormat()}&limit=1000");

        if (!string.IsNullOrWhiteSpace(code))
        {
            result.Append($"&code={code}");
        }

        return result.ToString();
    }

    private ImportAction FindImportAction(string actionName) =>
        _importActions.FirstOrDefault(a => a.Name != null &&
            a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));

    private bool ShouldActionRun(string actionName)
    {
        var action = FindImportAction(actionName);
        return !action.Equals(ImportAction.Default) && (action.Details!.Length > 0);
    }

    private void AddCallToUsage(string uri)
    {
        var cost = FindEndPointForUri(uri).Cost.GetValueOrDefault();

        lock (_locker)
        {
            _usage += cost;
            _available -= cost;
        }
    }

    private static EndPoint FindEndPointForUri(string? uri) =>
        _endPoints.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Uri) &&
            uri!.Contains(p.Uri, StringComparison.OrdinalIgnoreCase));

    private struct EndPoint(string? uri, string? category, int? cost)
    {
        public string? Uri = uri;
        public string? Category = category;
        public int? Cost = cost;
    }

    private readonly static EndPoint[] _endPoints = [
        new(Constants.Uris.User, Constants.Actions.User, 0),
        new(Constants.Uris.ExchangeList, Constants.Actions.ExchangeList, 1),
        new(Constants.Uris.ExchangeDetails, Constants.Actions.ExchangeDetails, 5),
        new(Constants.Uris.ExchangeSymbolList, Constants.Actions.ExchangeSymbolList, 1),
        new(Constants.Uris.Eod, Constants.Actions.EndOfDayPrices, 1),
        new(Constants.Uris.BulkEod, Constants.Actions.BulkEndOfDayPrices, 100),
        new(Constants.Uris.Dividends, Constants.Actions.Dividends, 1),
        new(Constants.Uris.Splits, Constants.Actions.Splits, 1),
        new(Constants.Uris.Calendar, Constants.Actions.CalendarEarnings, 1),
        new(Constants.Uris.Fundamentals, Constants.Actions.Fundamentals, 10),
        new(Constants.Uris.InsiderTransactions, Constants.Actions.InsiderTransactions, 10)
    ];

    public static class Constants
    {
        public static class Actions
        {
            public const string BulkDividends = "Bulk Dividends";
            public const string BulkEndOfDayPrices = "Bulk EOD Prices";
            public const string BulkSplits = "Bulk Splits";
            public const string CalendarEarnings = "Calendar Earnings";
            public const string CalendarIpos = "Calendar Ipos";
            public const string CalendarSplits = "Calendar Splits";
            public const string CalendarTrends = "Calendar Trends";
            public const string Dividends = "Dividends";
            public const string EndOfDayPrices = "EOD Prices";
            public const string ExchangeDetails = "Exchange Details";
            public const string ExchangeList = "Exchange List";
            public const string ExchangeSymbolList = "Exchange Symbol List";
            public const string Fundamentals = "Fundamentals";
            public const string InsiderTransactions = "Insider Transactions";
            public const string Prices = "Prices";
            public const string Purge = "Purge";
            public const string Splits = "Splits";
            public const string User = "User";

            public static bool ValueExists(string? text, bool caseSensitive = false)
            {
                if (text == null)
                {
                    return false;
                }

                var fields = typeof(Actions).GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (var field in fields)
                {
                    if ((caseSensitive && text.Equals(field.GetValue(null)?.ToString())) ||
                        !caseSensitive && text.Equals(field.GetValue(null)?.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static class Uris
        {
            public const string Base = "https://eodhd.com/api/";
            public const string BulkEod = "eod-bulk-last-day/";
            public const string Calendar = "calendar/";
            public const string Dividends = "div/";
            public const string Eod = "eod/";
            public const string ExchangeDetails = "exchange-details/";
            public const string ExchangeList = "exchanges-list/";
            public const string ExchangeSymbolList = "exchange-symbol-list/";
            public const string Fundamentals = "fundamentals/";
            public const string InsiderTransactions = "insider-transactions/";
            public const string Splits = "splits/";
            public const string User = "user/";
        }

        public static class OptionKeys
        {
            public const string MaxParallelization = "Max Parallelization";
            public const string Bulk = "Bulk";
        }
    }

    public class ImportConfigfile(IDictionary<string, string> importActions,
        IDictionary<string, string>? exchanges,
        IDictionary<string, string>? symbolTypes,
        IDictionary<string, string>? options,
        IDictionary<string, string>? dateRanges)
    {
        [JsonPropertyName("Import Actions")]
        public IDictionary<string, string> ImportActions { get; set; } = importActions;

        public IDictionary<string, string>? Exchanges { get; set; } = exchanges;

        [JsonPropertyName("Symbol Types")]
        public IDictionary<string, string>? SymbolTypes { get; set; } = symbolTypes;

        public IDictionary<string, string>? Options { get; set; } = options;

        [JsonPropertyName("Date Ranges")]
        public IDictionary<string, string>? DateRanges { get; set; } = dateRanges;
    }

    public class DataImportConfiguration(
        string source,
        string? apiKey,
        IDictionary<string, string>? importActions,
        IDictionary<string, string>? exchanges,
        IDictionary<string, string>? symbolTypes,
        IDictionary<string, string>? options,
        IDictionary<string, string>? dateRanges)
    {
        public string? ApiKey { get; } = apiKey;
        public string Source { get; } = source;
        public IDictionary<string, string> Actions { get; } = importActions ?? new Dictionary<string, string>();
        public IDictionary<string, string>? Exchanges { get; } = exchanges;
        public IDictionary<string, string>? SymbolTypes { get; } = symbolTypes;
        public IDictionary<string, string>? Options { get; } = options;
        public IDictionary<string, string>? DateRanges { get; } = dateRanges;

        internal static ReadOnlyDictionary<string, string[]> CreateDictionary(IDictionary<string, string>? dict)
        {
            var result = new Dictionary<string, string[]>(dict?.Keys.Count ?? 0);

            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    string val = kvp.Value;
                    string[] vals = string.IsNullOrWhiteSpace(val)
                        ? []
                        : val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    result.Add(kvp.Key.Trim(), vals);
                }
            }

            return new ReadOnlyDictionary<string, string[]>(result);
        }
    }
}