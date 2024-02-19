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
created_ticks_utc, updated_ticks_utc)
VALUES (
@Id, @Name, @Type, @Source, @Description, @EntryPricePoint,
@TargetUpPercentage, @TargetUpPricePoint,
@TargetDownPercentage, @TargetDownPricePoint,
@CreatedTicksUtc, @UpdatedTicksUtc
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
updated_ticks_utc AS UpdatedTicksUtc
FROM public.backtests",
            _ => ThrowSqlNotImplemented()
        };

        public string UpsertBacktestResult => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.backtest_results(
id, backtest_id, code, entry_date, 
entry_price_point, entry_price,
result_up_date, result_up_price_point, result_up_price, 
result_down_date, result_down_price_point, result_down_price,
result_direction, 
result_duration_trading_days, result_duration_calendar_days, 
created_ticks_utc, updated_ticks_utc, process_id)
VALUES
(
@Id, @BacktestId, @Code, @EntryDate,
@EntryPricePoint, @EntryPrice,
@ResultUpDate, @ResultUpPricePoint, @ResultUpPrice,
@ResultDownDate, @ResultDownPricePoint, @ResultDownPrice,
@ResultDirection,
@ResultDurationTradingDays, @ResultDurationCalendarDays,
@CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId
)
ON CONFLICT (id) DO UPDATE SET
backtest_id = EXCLUDED.backtest_id,
code = EXCLUDED.code,
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
updated_ticks_utc = EXCLUDED.updated_ticks_utc,
process_id = EXCLUDED.process_id",
            _ => ThrowSqlNotImplemented()
        };

        public string FetchBacktestResult => _dbDef.Engine switch {
            DatabaseEngine.PostgreSql => @"SELECT id, backtest_id AS BacktestId, code,
entry_date AS EntryDate, entry_price_point AS EntryPricePoint, entry_price AS EntryPrice,
result_up_date AS ResultUpDate, result_up_price_point AS ResultUpPricePoint, result_up_price AS ResultUpPrice,
result_down_date AS ResultDownDate, result_down_price_point AS ResultDownPricePoint, result_down_price AS ResultDownPrice,
result_direction AS ResultDirection,
result_duration_trading_days AS ResultDurationTradingDays,
result_duration_calendar_days AS ResultDurationCalendarDays,
created_ticks_utc AS CreatedTicksUtc,
updated_ticks_utc AS UpdatedTicksUtc,
process_id AS ProcessId
FROM public.backtest_results",
            _ => ThrowSqlNotImplemented()
        };




        //        public string Upsert => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"INSERT INTO public.eod_prices (
        //source, code, date_eod, open, high, low, close, volume, created_ticks_utc, updated_ticks_utc, process_id)
        //VALUES (@Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, @CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId)
        //ON CONFLICT (source, code, date_eod) DO UPDATE
        //SET open = EXCLUDED.open,
        //high = EXCLUDED.high,
        //low = EXCLUDED.low,
        //close = EXCLUDED.close,
        //volume = EXCLUDED.volume,
        //updated_ticks_utc = EXCLUDED.updated_ticks_utc,
        //process_id = EXCLUDED.process_id",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string Fetch => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"SELECT
        //source, code, date_eod AS DateEod, open, high, low, close, volume,
        //created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc,
        //process_id AS ProcessId
        //FROM public.eod_prices",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string Delete => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_prices",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string DeleteEodPricesWithAdjustedPrices => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_prices
        //WHERE (source, code, date_eod) IN
        //(SELECT source, code, date_eod FROM eod_adjusted_prices)",
        //            _ => ThrowSqlNotImplemented()
        //        };
        //    }

        //    internal class AdjustedEodPricesInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
        //    {
        //        public string Upsert => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"INSERT INTO public.eod_adjusted_prices (
        //source, code, date_eod, open, high, low, close, volume, factor, created_ticks_utc, updated_ticks_utc, process_id)
        //VALUES (@Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, @Factor, @CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId)
        //ON CONFLICT (source, code, date_eod) DO UPDATE
        //SET open = EXCLUDED.open,
        //high = EXCLUDED.high,
        //low = EXCLUDED.low,
        //close = EXCLUDED.close,
        //volume = EXCLUDED.volume,
        //updated_ticks_utc = EXCLUDED.updated_ticks_utc,
        //process_id = EXCLUDED.process_id",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string Fetch => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"SELECT
        //source, code, date_eod AS DateEod, open, high, low, close, volume, factor,
        //created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc,
        //process_id AS ProcessId
        //FROM public.eod_adjusted_prices",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string FetchAllAdjustedSymbolsForSource => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"SELECT distinct code FROM eod_adjusted_prices WHERE source = @Source",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string Delete => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_adjusted_prices",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string MigratePricesWithoutSplitsToAdjustedPrices => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"INSERT INTO public.eod_adjusted_prices
        //(source, code, date_eod, open, high, low, close, volume,
        //factor,
        //created_ticks_utc,
        //updated_ticks_utc,
        //process_id)
        //SELECT P.source, P.code, P.date_eod, open, high, low, close, volume,
        //1,
        //(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000,
        //(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000,
        //P.process_id
        //FROM public.eod_prices P
        //LEFT JOIN public.splits S ON P.source = S.source AND P.code = S.code
        //WHERE S.source IS NULL
        //ON CONFLICT (source, code, date_eod) DO UPDATE
        //SET open = EXCLUDED.open,
        //high = EXCLUDED.high,
        //low = EXCLUDED.low,
        //close = EXCLUDED.close,
        //volume = EXCLUDED.volume,
        //factor = EXCLUDED.factor,
        //updated_ticks_utc = EXCLUDED.updated_ticks_utc,
        //process_id = EXCLUDED.process_id",
        //            _ => ThrowSqlNotImplemented()
        //        };
        //    }

        //    internal class SplitsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
        //    {
        //        public string Upsert => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"INSERT INTO public.splits(
        //source, code, date_split, before_split, after_split, factor, created_ticks_utc, updated_ticks_utc, process_id)
        //VALUES (@Source, @Code, @SplitDate, @Before, @After, @Factor, @CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId)
        //ON CONFLICT (source, code, date_split) DO UPDATE
        //SET 
        //before_split = EXCLUDED.before_split,
        //after_split = EXCLUDED.after_split,
        //factor = EXCLUDED.factor,
        //updated_ticks_utc = EXCLUDED.updated_ticks_utc,
        //process_id = EXCLUDED.process_id",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string Fetch => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"SELECT
        //source, code, date_split AS SplitDate, before_split AS Before, after_split AS After,
        //created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc, process_id AS ProcessId
        //FROM public.splits",
        //            _ => ThrowSqlNotImplemented()
        //        };

        //        public string DeleteForSource => _dbDef.Engine switch
        //        {
        //            DatabaseEngine.PostgreSql => @"DELETE FROM public.splits WHERE source = @Source",
        //            _ => ThrowSqlNotImplemented()
        //        };
    }
}