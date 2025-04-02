using System.Reflection;

namespace Kyna.Infrastructure.Database;

public static class SqlKeys
{
    /// <summary>
    /// Fetches all possible keys.
    /// </summary>
    /// <returns>A collection of keys.</returns>
    public static IEnumerable<string?> GetKeys()
    {
        foreach (var f in typeof(SqlKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.IsLiteral && f.DeclaringType == typeof(SqlKeys) && f.FieldType == typeof(string))
            {
                yield return (string?)f.GetRawConstantValue();
            }
        }
    }

    public const string InsertApiTransaction = nameof(InsertApiTransaction);
    public const string FetchApiTransaction = nameof(FetchApiTransaction);
    public const string FetchApiResponseBodyForId = nameof(FetchApiResponseBodyForId);
    public const string FetchApiTransactionsForMigration = nameof(FetchApiTransactionsForMigration);
    public const string DeleteApiTransactions = nameof(DeleteApiTransactions);
    public const string DeleteApiTransactionsForSource = nameof(DeleteApiTransactionsForSource);

    public const string UpsertRemoteFile = nameof(UpsertRemoteFile);
    public const string FetchRemoteFiles = nameof(FetchRemoteFiles);
    public const string DeleteRemoteFilesForSource = nameof(DeleteRemoteFilesForSource);

    public const string UpsertBacktest = nameof(UpsertBacktest);
    public const string FetchBacktest = nameof(FetchBacktest);
    public const string UpsertBacktestResult = nameof(UpsertBacktestResult);
    public const string FetchBacktestResult = nameof(FetchBacktestResult);
    public const string UpsertBacktestStats = nameof(UpsertBacktestStats);
    public const string FetchBacktestStats = nameof(FetchBacktestStats);
    public const string DeleteBacktestStats = nameof(DeleteBacktestStats);
    public const string FetchBacktestResultInfo = nameof(FetchBacktestResultInfo);
    public const string FetchBacktestSignalCounts = nameof(FetchBacktestSignalCounts);
    public const string FetchBacktestSignalSummary = nameof(FetchBacktestSignalSummary);
    public const string FetchBacktestSignalDetails = nameof(FetchBacktestSignalDetails);
    public const string FetchBacktestsProcessIdInfo = nameof(FetchBacktestsProcessIdInfo);
    public const string DeleteBacktestsForProcessId = nameof(DeleteBacktestsForProcessId);

    public const string UpsertEodPrice = nameof(UpsertEodPrice);
    public const string FetchEodPrices = nameof(FetchEodPrices);
    public const string FetchCodesWithSplits = nameof(FetchCodesWithSplits);
    public const string CopyPricesWithoutSplitsToAdjustedPrices = nameof(CopyPricesWithoutSplitsToAdjustedPrices);
    public const string FetchCodesWithoutSplits = nameof(FetchCodesWithoutSplits);
    public const string DeleteEodPrices = nameof(DeleteEodPrices);
    public const string DeleteEodPricesWithAdjustedPrices = nameof(DeleteEodPricesWithAdjustedPrices);

    public const string InsertLog = nameof(InsertLog);
    public const string FetchLogs = nameof(FetchLogs);
    public const string DeleteLogs = nameof(DeleteLogs);
    public const string InsertAppEvent = nameof(InsertAppEvent);
    public const string FetchAppEvents = nameof(FetchAppEvents);
    public const string DeleteAppEvents = nameof(DeleteAppEvents);

    public const string UpsertAdjustedEodPrice = nameof(UpsertAdjustedEodPrice);
    public const string FetchAdjustedEodPrices = nameof(FetchAdjustedEodPrices);
    public const string FetchAllAdjustedSymbolsForSource = nameof(FetchAllAdjustedSymbolsForSource);
    public const string DeleteAdjustedEodPrices = nameof(DeleteAdjustedEodPrices);
    public const string MigratePricesWithoutSplitsToAdjustedPrices = nameof(MigratePricesWithoutSplitsToAdjustedPrices);
    public const string FetchAdjustedCodesAndCounts = nameof(FetchAdjustedCodesAndCounts);
    public const string FetchAdjustedCodesAndDates = nameof(FetchAdjustedCodesAndDates);

    public const string UpsertSplit = nameof(UpsertSplit);
    public const string FetchSplits = nameof(FetchSplits);
    public const string DeleteSplitsForSource = nameof(DeleteSplitsForSource);

    public const string UpsertDividend = nameof(UpsertDividend);
    public const string FetchDividends = nameof(FetchDividends);
    public const string DeleteDividendsForSource = nameof(DeleteDividendsForSource);

    public const string InsertBasicEntity = nameof(InsertBasicEntity);
    public const string UpdateSplitsInEntities = nameof(UpdateSplitsInEntities);
    public const string HydrateMissingEntities = nameof(HydrateMissingEntities);
    public const string SetSplitIndicatorForEntities = nameof(SetSplitIndicatorForEntities);
    public const string SetPriceActionIndicatorForEntities = nameof(SetPriceActionIndicatorForEntities);
    public const string SetLastPriceActionForEntities = nameof(SetLastPriceActionForEntities);
    public const string DeleteEntitiesWithoutTypesOrPriceActions = nameof(DeleteEntitiesWithoutTypesOrPriceActions);
    public const string UpsertEntity = nameof(UpsertEntity);
    public const string FetchEntity = nameof(FetchEntity);
    public const string DeleteEntityForSourceAndCode = nameof(DeleteEntityForSourceAndCode);

}