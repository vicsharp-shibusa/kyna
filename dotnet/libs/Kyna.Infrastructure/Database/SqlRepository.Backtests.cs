namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    internal class BacktestsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string UpsertBacktest => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.backtests(
id, name, type, source, description, entry_price_point,
target_up_percentage, target_up_price_point,
target_down_percentage, target_down_price_point,
created_ticks_utc, updated_ticks_utc, process_id)
VALUES (
@Id, @Name, @Type, @Source, @Description, @EntryPricePoint,
@TargetUpPercentage, @TargetUpPricePoint,
@TargetDownPercentage, @TargetDownPricePoint,
@CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId
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
process_id = EXCLUDED.process_id,
updated_ticks_utc = EXCLUDED.updated_ticks_utc",
            _ => ThrowSqlNotImplemented()
        };
        public string FetchBacktest => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT
id, name, type, source, description,
entry_price_point AS EntryPricePoint,
target_up_percentage AS TargetUpPercentage,
target_up_price_point AS TargetUpPricePoint,
target_down_percentage AS TargetDownPercentage,
target_down_price_point AS TargetDownPricePoint,
created_ticks_utc AS CreatedTicksUtc,
updated_ticks_utc AS UpdatedTicksUtc,
process_id AS ProcessId
FROM public.backtests",
            _ => ThrowSqlNotImplemented()
        };
        public string UpsertBacktestResult => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.backtest_results(
id, backtest_id, signal_name, code, industry, sector, entry_date, 
entry_price_point, entry_price,
result_up_date, result_up_price_point, result_up_price, 
result_down_date, result_down_price_point, result_down_price,
result_direction, 
result_duration_trading_days, result_duration_calendar_days, 
created_ticks_utc, updated_ticks_utc)
VALUES
(
@Id, @BacktestId, @SignalName, @Code, @Industry, @Sector, @EntryDate,
@EntryPricePoint, @EntryPrice,
@ResultUpDate, @ResultUpPricePoint, @ResultUpPrice,
@ResultDownDate, @ResultDownPricePoint, @ResultDownPrice,
@ResultDirection,
@ResultDurationTradingDays, @ResultDurationCalendarDays,
@CreatedTicksUtc, @UpdatedTicksUtc
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
updated_ticks_utc = EXCLUDED.updated_ticks_utc",
            _ => ThrowSqlNotImplemented()
        };
        public string FetchBacktestResult => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT id, backtest_id AS BacktestId, signal_name AS SignalName, code, industry, sector,
entry_date AS EntryDate, entry_price_point AS EntryPricePoint, entry_price AS EntryPrice,
result_up_date AS ResultUpDate, result_up_price_point AS ResultUpPricePoint, result_up_price AS ResultUpPrice,
result_down_date AS ResultDownDate, result_down_price_point AS ResultDownPricePoint, result_down_price AS ResultDownPrice,
result_direction AS ResultDirection,
result_duration_trading_days AS ResultDurationTradingDays,
result_duration_calendar_days AS ResultDurationCalendarDays,
created_ticks_utc AS CreatedTicksUtc,
updated_ticks_utc AS UpdatedTicksUtc
FROM public.backtest_results",
            _ => ThrowSqlNotImplemented()
        };
        public string UpsertBacktestStats => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
INSERT INTO public.backtest_stats(
source, signal_name, category, sub_category, 
number_entities, number_signals, 
success_percentage, success_criterion, 
success_duration_trading_days, success_duration_calendar_days, 
process_id, created_ticks_utc, updated_ticks_utc)
VALUES (@Source, @SignalName, @Category, @SubCategory,
@NumberEntities, @NumberSignals,
@SuccessPercentage, @SuccessCriterion,
@SuccessDurationTradingDays, @SuccessDurationCalendarDays,
@ProcessId, @CreatedTicksUtc, @UpdatedTicksUtc)
ON CONFLICT (source, signal_name, category, sub_category) DO UPDATE SET
number_entities = EXCLUDED.number_entities,
number_signals = EXCLUDED.number_signals,
success_percentage = EXCLUDED.success_percentage,
success_criterion = EXCLUDED.success_criterion,
success_duration_trading_days = EXCLUDED.success_duration_trading_days,
success_duration_calendar_days = EXCLUDED.success_duration_calendar_days,
process_id = EXCLUDED.process_id,
updated_ticks_utc = EXCLUDED.updated_ticks_utc
",
            _ => ThrowSqlNotImplemented()
        };
        public string FetchBacktestStats => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
SELECT source, signal_name AS SignalName, category, sub_category AS SubCategory, 
number_entities AS NumberEntities, number_signals AS NumberSignals, 
success_percentage AS SuccessPercentage, success_criterion AS SuccessCriterion, 
success_duration_trading_days AS SuccessDurationTradingDays,
success_duration_calendar_days AS SuccessDurationCalendarDays, 
created_ticks_utc AS CreatedTicksUtc,
updated_ticks_utc AS UpdatedTicksUtc,
process_id AS ProcessId
FROM public.backtest_stats",
            _ => ThrowSqlNotImplemented()
        };
        public string DeleteBacktestStats => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
DELETE FROM public.backtest_stats
",
            _ => ThrowSqlNotImplemented()
        };
        public string FetchBacktestResultInfo => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
SELECT signal_name AS SignalName, code, industry, sector,
result_direction AS ResultDirection, 
result_duration_trading_days AS ResultDurationTradingDays,
result_duration_calendar_days AS ResultDurationCalendarDays
FROM public.backtest_results
WHERE backtest_id = @BacktestId
",
            _ => ThrowSqlNotImplemented()
        };
    }
}