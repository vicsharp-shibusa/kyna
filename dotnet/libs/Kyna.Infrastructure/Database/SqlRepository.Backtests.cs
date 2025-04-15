namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetBacktestSql()
    {
        // Upsert a backtest record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertBacktest, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.backtests(
    id, name, type, source, description, entry_price_point,
    target_up_percentage, target_up_price_point,
    target_down_percentage, target_down_price_point,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms, process_id)
VALUES (
    @Id, @Name, @Type, @Source, @Description, @EntryPricePoint,
    @TargetUpPercentage, @TargetUpPricePoint,
    @TargetDownPercentage, @TargetDownPricePoint,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs, @ProcessId
)
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    type = EXCLUDED.type,
    source = EXCLUDED.source,
    description = EXCLUDED.description,
    entry_price_point = EXCLUDED.entry_price_point,
    target_up_percentage = EXCLUDED.target_up_percentage,
    target_up_price_point = EXCLUDED.target_up_price_point,
    target_down_percentage = EXCLUDED.target_down_percentage,
    target_down_price_point = EXCLUDED.target_down_price_point,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms,
    process_id = EXCLUDED.process_id");

        // Fetch all backtests
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktest, DatabaseEngine.PostgreSql),
            @"
SELECT
    id, name, type, source, description,
    entry_price_point AS EntryPricePoint,
    target_up_percentage AS TargetUpPercentage,
    target_up_price_point AS TargetUpPricePoint,
    target_down_percentage AS TargetDownPercentage,
    target_down_price_point AS TargetDownPricePoint,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    process_id AS ProcessId
FROM public.backtests");

        // Upsert a backtest result
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertBacktestResult, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.backtest_results(
    id, backtest_id, signal_name, code, industry, sector, entry_date,
    entry_price_point, entry_price,
    result_up_date, result_up_price_point, result_up_price,
    result_down_date, result_down_price_point, result_down_price,
    result_direction, result_duration_trading_days, result_duration_calendar_days,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms, process_id)
VALUES (
    @Id, @BacktestId, @SignalName, @Code, @Industry, @Sector, @EntryDate,
    @EntryPricePoint, @EntryPrice,
    @ResultUpDate, @ResultUpPricePoint, @ResultUpPrice,
    @ResultDownDate, @ResultDownPricePoint, @ResultDownPrice,
    @ResultDirection, @ResultDurationTradingDays, @ResultDurationCalendarDays,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs, @ProcessId
)
ON CONFLICT (id) DO UPDATE SET
    backtest_id = EXCLUDED.backtest_id,
    signal_name = EXCLUDED.signal_name,
    code = EXCLUDED.code,
    industry = EXCLUDED.industry,
    sector = EXCLUDED.sector,
    entry_date = EXCLUDED.entry_date,
    entry_price_point = EXCLUDED.entry_price_point,
    entry_price = EXCLUDED.entry_price,
    result_up_date = EXCLUDED.result_up_date,
    result_up_price_point = EXCLUDED.result_up_price_point,
    result_up_price = EXCLUDED.result_up_price,
    result_down_date = EXCLUDED.result_down_date,
    result_down_price_point = EXCLUDED.result_down_price_point,
    result_down_price = EXCLUDED.result_down_price,
    result_direction = EXCLUDED.result_direction,
    result_duration_trading_days = EXCLUDED.result_duration_trading_days,
    result_duration_calendar_days = EXCLUDED.result_duration_calendar_days,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms,
    process_id = @ProcessId");

        // Fetch all backtest results
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestResult, DatabaseEngine.PostgreSql),
            @"
SELECT
    id, backtest_id AS BacktestId, signal_name AS SignalName, code, industry, sector,
    entry_date AS EntryDate, entry_price_point AS EntryPricePoint, entry_price AS EntryPrice,
    result_up_date AS ResultUpDate, result_up_price_point AS ResultUpPricePoint, result_up_price AS ResultUpPrice,
    result_down_date AS ResultDownDate, result_down_price_point AS ResultDownPricePoint, result_down_price AS ResultDownPrice,
    result_direction AS ResultDirection,
    result_duration_trading_days AS ResultDurationTradingDays,
    result_duration_calendar_days AS ResultDurationCalendarDays,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    process_id AS ProcessId
FROM public.backtest_results");

        // Upsert backtest stats
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertBacktestStats, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.backtest_stats(
    backtest_id, source, signal_name, category, sub_category,
    number_entities, number_signals,
    success_percentage, success_criterion,
    success_duration_trading_days, success_duration_calendar_days,
    created_at, updated_at, created_at_unix_ms, updated_at_unix_ms, process_id)
VALUES (
    @BacktestId, @Source, @SignalName, @Category, @SubCategory,
    @NumberEntities, @NumberSignals,
    @SuccessPercentage, @SuccessCriterion,
    @SuccessDurationTradingDays, @SuccessDurationCalendarDays,
    @CreatedAt, @UpdatedAt, @CreatedAtUnixMs, @UpdatedAtUnixMs, @ProcessId
)
ON CONFLICT (backtest_id, source, signal_name, category, sub_category) DO UPDATE SET
    number_entities = EXCLUDED.number_entities,
    number_signals = EXCLUDED.number_signals,
    success_percentage = EXCLUDED.success_percentage,
    success_criterion = EXCLUDED.success_criterion,
    success_duration_trading_days = EXCLUDED.success_duration_trading_days,
    success_duration_calendar_days = EXCLUDED.success_duration_calendar_days,
    updated_at = EXCLUDED.updated_at,
    updated_at_unix_ms = EXCLUDED.updated_at_unix_ms,
    process_id = EXCLUDED.process_id");

        // Fetch all backtest stats
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestStats, DatabaseEngine.PostgreSql),
            @"
SELECT
    backtest_id AS BacktestId, source, signal_name AS SignalName, category, sub_category AS SubCategory,
    number_entities AS NumberEntities, number_signals AS NumberSignals,
    success_percentage AS SuccessPercentage, success_criterion AS SuccessCriterion,
    success_duration_trading_days AS SuccessDurationTradingDays,
    success_duration_calendar_days AS SuccessDurationCalendarDays,
    created_at_unix_ms AS CreatedAtUnixMs,
    updated_at_unix_ms AS UpdatedAtUnixMs,
    process_id AS ProcessId
FROM public.backtest_stats");

        // Delete all backtest stats (use with caution)
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteBacktestStats, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.backtest_stats");

        // Fetch backtest result info for a specific backtest
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestResultInfo, DatabaseEngine.PostgreSql),
            @"
SELECT
    signal_name AS SignalName, code, industry, sector,
    result_direction AS ResultDirection,
    result_duration_trading_days AS ResultDurationTradingDays,
    result_duration_calendar_days AS ResultDurationCalendarDays
FROM public.backtest_results
WHERE backtest_id = @BacktestId
ORDER BY signal_name, code");

        // Fetch signal counts for a process ID
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestSignalCounts, DatabaseEngine.PostgreSql),
            @"
SELECT
    R.backtest_id AS BacktestId,
    signal_name AS SignalName,
    result_direction AS ResultDirection,
    COUNT(*) AS Count
FROM public.backtest_results R
JOIN public.backtests B ON B.id = R.backtest_id
WHERE B.process_id = @ProcessId
GROUP BY R.backtest_id, signal_name, result_direction
ORDER BY R.backtest_id, signal_name, result_direction");

        // Fetch signal summary for a specific backtest and signal
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestSignalSummary, DatabaseEngine.PostgreSql),
            @"
SELECT
    backtest_id AS BacktestId, signal_name AS Name, category, sub_category AS SubCategory,
    number_signals AS NumberSignals,
    success_percentage AS SuccessPercentage,
    success_duration_calendar_days AS SuccessDuration
FROM public.backtest_stats
WHERE backtest_id = @BacktestId
AND signal_name = @SignalName
ORDER BY success_percentage DESC, success_duration_calendar_days ASC");

        // Fetch detailed signal results for a process ID, backtest ID, and signal name
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestSignalDetails, DatabaseEngine.PostgreSql),
            @"
SELECT
    R.backtest_id AS BacktestId,
    R.signal_name AS Name,
    R.code, R.industry, R.sector,
    R.entry_date AS EntryDate,
    R.entry_price_point AS EntryPricePoint,
    R.entry_price AS EntryPrice,
    R.result_up_date AS ResultUpDate, R.result_up_price_point AS ResultUpPricePoint, R.result_up_price AS ResultUpPrice,
    R.result_down_date AS ResultDownDate, R.result_down_price_point AS ResultDownPricePoint, R.result_down_price AS ResultDownPrice,
    R.result_direction AS ResultDirection,
    R.result_duration_trading_days AS TradingDays,
    R.result_duration_calendar_days AS CalendarDays
FROM public.backtest_results R
JOIN public.backtests B ON B.id = R.backtest_id
WHERE B.process_id = @ProcessId AND R.signal_name = @SignalName AND R.backtest_id = @BacktestId
ORDER BY R.code, R.entry_date");

        // Fetch process ID info with backtest counts and date ranges
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SelectBacktestsProcessIdInfo, DatabaseEngine.PostgreSql),
            @"
SELECT
    B.process_id AS ProcessId,
    COUNT(B.id) AS BacktestCount,
    MIN(B.created_at) AS MinDate,
    MAX(B.created_at) AS MaxDate
FROM public.backtests B
GROUP BY B.process_id
ORDER BY MIN(B.created_at) DESC");

        // Delete backtests, results, and stats for a process ID
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteBacktestsForProcessId, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.backtest_stats
WHERE process_id = @ProcessId;
DELETE FROM public.backtest_results
WHERE backtest_id IN (SELECT id FROM public.backtests WHERE process_id = @ProcessId);
DELETE FROM public.backtests
WHERE process_id = @ProcessId");
    }
}