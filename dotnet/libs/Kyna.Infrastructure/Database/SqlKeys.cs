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

    // Logs
    public const string DeleteAppEvents = nameof(DeleteAppEvents);
    public const string DeleteLogs = nameof(DeleteLogs);
    public const string InsertAppEvent = nameof(InsertAppEvent);
    public const string InsertLog = nameof(InsertLog);
    public const string SelectAppEvents = nameof(SelectAppEvents);
    public const string SelectLogs = nameof(SelectLogs);

    // API Transactions
    public const string DeleteApiTransactions = nameof(DeleteApiTransactions);
    public const string DeleteApiTransactionsForSource = nameof(DeleteApiTransactionsForSource);
    public const string InsertApiTransaction = nameof(InsertApiTransaction);
    public const string SelectApiResponseBodyForId = nameof(SelectApiResponseBodyForId);
    public const string SelectApiTransaction = nameof(SelectApiTransaction);
    public const string SelectApiTransactionsForMigration = nameof(SelectApiTransactionsForMigration);

    // Remote files
    public const string DeleteRemoteFilesForSource = nameof(DeleteRemoteFilesForSource);
    public const string MarkRemoteFileAsMigrated = nameof(MarkRemoteFileAsMigrated);
    public const string SelectRemoteFiles = nameof(SelectRemoteFiles);
    public const string UpsertRemoteFile = nameof(UpsertRemoteFile);

    // Prices
    public const string CopyPricesWithoutSplitsToAdjustedPrices = nameof(CopyPricesWithoutSplitsToAdjustedPrices);
    public const string DeleteEodPrices = nameof(DeleteEodPrices);
    public const string DeleteEodPricesWithAdjustedPrices = nameof(DeleteEodPricesWithAdjustedPrices);
    public const string SelectCodesWithoutSplits = nameof(SelectCodesWithoutSplits);
    public const string SelectCodesWithSplits = nameof(SelectCodesWithSplits);
    public const string SelectEodPrices = nameof(SelectEodPrices);
    public const string UpsertEodPrice = nameof(UpsertEodPrice);

    // Adjusted Prices
    public const string DeleteEodAdjustedPrices = nameof(DeleteEodAdjustedPrices);
    public const string MigratePricesWithoutSplitsToAdjustedPrices = nameof(MigratePricesWithoutSplitsToAdjustedPrices);
    public const string SelectAdjustedCodesAndCounts = nameof(SelectAdjustedCodesAndCounts);
    public const string SelectAdjustedCodesAndDates = nameof(SelectAdjustedCodesAndDates);
    public const string SelectAllAdjustedSymbolsForSource = nameof(SelectAllAdjustedSymbolsForSource);
    public const string SelectEodAdjustedPrices = nameof(SelectEodAdjustedPrices);
    public const string UpsertEodAdjustedPrice = nameof(UpsertEodAdjustedPrice);

    // Splits
    public const string DeleteSplitsForSource = nameof(DeleteSplitsForSource);
    public const string SelectSplits = nameof(SelectSplits);
    public const string UpsertSplit = nameof(UpsertSplit);

    // Dividends
    public const string DeleteDividendsForSource = nameof(DeleteDividendsForSource);
    public const string SelectDividends = nameof(SelectDividends);
    public const string UpsertDividend = nameof(UpsertDividend);

    // Entities
    public const string DeleteEntitiesWithoutTypesOrPriceActions = nameof(DeleteEntitiesWithoutTypesOrPriceActions);
    public const string DeleteEntityForSourceAndCode = nameof(DeleteEntityForSourceAndCode);
    public const string HydrateMissingEntities = nameof(HydrateMissingEntities);
    public const string InsertBasicEntity = nameof(InsertBasicEntity);
    public const string SelectEntity = nameof(SelectEntity);
    public const string SetLastPriceActionForEntities = nameof(SetLastPriceActionForEntities);
    public const string SetPriceActionIndicatorForEntities = nameof(SetPriceActionIndicatorForEntities);
    public const string SetSplitIndicatorForEntities = nameof(SetSplitIndicatorForEntities);
    public const string UpdateSplitsInEntities = nameof(UpdateSplitsInEntities);
    public const string UpsertEntity = nameof(UpsertEntity);

    // Backtests
    public const string DeleteBacktestsForProcessId = nameof(DeleteBacktestsForProcessId);
    public const string DeleteBacktestStats = nameof(DeleteBacktestStats);
    public const string SelectBacktest = nameof(SelectBacktest);
    public const string SelectBacktestResult = nameof(SelectBacktestResult);
    public const string SelectBacktestResultInfo = nameof(SelectBacktestResultInfo);
    public const string SelectBacktestSignalCounts = nameof(SelectBacktestSignalCounts);
    public const string SelectBacktestSignalDetails = nameof(SelectBacktestSignalDetails);
    public const string SelectBacktestSignalSummary = nameof(SelectBacktestSignalSummary);
    public const string SelectBacktestsProcessIdInfo = nameof(SelectBacktestsProcessIdInfo);
    public const string SelectBacktestStats = nameof(SelectBacktestStats);
    public const string UpsertBacktest = nameof(UpsertBacktest);
    public const string UpsertBacktestResult = nameof(UpsertBacktestResult);
    public const string UpsertBacktestStats = nameof(UpsertBacktestStats);

    // Stats
    public const string SelectStat = nameof(SelectStat);
    public const string SelectStatBuild = nameof(SelectStatBuild);
    public const string SelectStatDetail = nameof(SelectStatDetail);
    public const string UpsertStat = nameof(UpsertStat);
    public const string UpsertStatBuild = nameof(UpsertStatBuild);
    public const string UpsertStatDetail = nameof(UpsertStatDetail);

    // Utility
    public const string DeleteLeadingPriceGaps = nameof(DeleteLeadingPriceGaps);
}