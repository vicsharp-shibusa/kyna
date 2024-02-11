namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    internal class EodPricesInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Upsert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.eod_prices (
source, code, date_eod, open, high, low, close, volume, created_ticks_utc, updated_ticks_utc, process_id)
VALUES (@Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, @CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId)
ON CONFLICT (source, code, date_eod) DO UPDATE
SET open = EXCLUDED.open,
high = EXCLUDED.high,
low = EXCLUDED.low,
close = EXCLUDED.close,
volume = EXCLUDED.volume,
updated_ticks_utc = EXCLUDED.updated_ticks_utc,
process_id = EXCLUDED.process_id",
            _ => ThrowSqlNotImplemented()
        };

        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT
source, code, date_eod AS DateEod, open, high, low, close, volume,
created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc,
process_id AS ProcessId
FROM public.eod_prices",
            _ => ThrowSqlNotImplemented()
        };

        public string Delete => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_prices",
            _ => ThrowSqlNotImplemented()
        };

        //public string CountPricesWithoutSplits => _dbDef.Engine switch
        //{
        //    DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_adjusted_prices",
        //    _ => ThrowSqlNotImplemented()
        //};

        public string DeleteEodPricesWithAdjustedPrices => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_prices
WHERE (source, code, date_eod) IN
(SELECT source, code, date_eod FROM eod_adjusted_prices)",
            _ => ThrowSqlNotImplemented()
        };
    }

    internal class AdjustedEodPricesInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Upsert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.eod_adjusted_prices (
source, code, date_eod, open, high, low, close, volume, factor, created_ticks_utc, updated_ticks_utc, process_id)
VALUES (@Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, @Factor, @CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId)
ON CONFLICT (source, code, date_eod) DO UPDATE
SET open = EXCLUDED.open,
high = EXCLUDED.high,
low = EXCLUDED.low,
close = EXCLUDED.close,
volume = EXCLUDED.volume,
updated_ticks_utc = EXCLUDED.updated_ticks_utc,
process_id = EXCLUDED.process_id",
            _ => ThrowSqlNotImplemented()
        };

        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT
source, code, date_eod AS DateEod, open, high, low, close, volume, factor,
created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc,
process_id AS ProcessId
FROM public.eod_adjusted_prices",
            _ => ThrowSqlNotImplemented()
        };

        public string Delete => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.eod_adjusted_prices",
            _ => ThrowSqlNotImplemented()
        };

        public string MigratePricesWithoutSplitsToAdjustedPrices => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.eod_adjusted_prices
(source, code, date_eod, open, high, low, close, volume,
factor,
created_ticks_utc,
updated_ticks_utc,
process_id)
SELECT P.source, P.code, P.date_eod, open, high, low, close, volume,
1,
(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000,
(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000,
P.process_id
FROM public.eod_prices P
LEFT JOIN public.splits S ON P.source = S.source AND P.code = S.code
WHERE S.source IS NULL
ON CONFLICT (source, code, date_eod) DO UPDATE
SET open = EXCLUDED.open,
high = EXCLUDED.high,
low = EXCLUDED.low,
close = EXCLUDED.close,
volume = EXCLUDED.volume,
factor = EXCLUDED.factor,
updated_ticks_utc = EXCLUDED.updated_ticks_utc,
process_id = EXCLUDED.process_id",
            _ => ThrowSqlNotImplemented()
        };
    }

    internal class SplitsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Upsert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.splits(
source, code, date_split, before_split, after_split, factor, created_ticks_utc, updated_ticks_utc, process_id)
VALUES (@Source, @Code, @SplitDate, @Before, @After, @Factor, @CreatedTicksUtc, @UpdatedTicksUtc, @ProcessId)
ON CONFLICT (source, code, date_split) DO UPDATE
SET 
before_split = EXCLUDED.before_split,
after_split = EXCLUDED.after_split,
factor = EXCLUDED.factor,
updated_ticks_utc = EXCLUDED.updated_ticks_utc,
process_id = EXCLUDED.process_id",
            _ => ThrowSqlNotImplemented()
        };

        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT
source, code, date_split AS SplitDate, before_split AS Before, after_split AS After,
created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc, process_id AS ProcessId
FROM public.splits",
            _ => ThrowSqlNotImplemented()
        };

        public string DeleteForSource => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.splits WHERE source = @Source",
            _ => ThrowSqlNotImplemented()
        };
    }
}