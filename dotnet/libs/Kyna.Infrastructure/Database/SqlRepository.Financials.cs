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

        public string FetchAllAdjustedSymbolsForSource => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"SELECT distinct code FROM eod_adjusted_prices WHERE source = @Source",
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
    internal class FundamentalsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string InsertBasicEntity => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.entities (
source, code, created_ticks_utc, updated_ticks_utc)
VALUES (
@Source, @Code, @CreatedTicksUtc, @UpdatedTicksUtc)
ON CONFLICT (source, code) DO NOTHING",
            _ => ThrowSqlNotImplemented()
        };

        public string UpdateSplitsInEntities => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"",
            _ => ThrowSqlNotImplemented()
        };

        public string HydrateMissingEntities => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
CREATE TEMPORARY TABLE tickers
(
	source TEXT NOT NULL,
	code TEXT NOT NULL,
	PRIMARY KEY (source, code)
);
INSERT INTO tickers
SELECT source, code FROM public.eod_prices
ON CONFLICT(source, code) DO NOTHING;
INSERT INTO tickers
SELECT source, code FROM public.eod_adjusted_prices
ON CONFLICT(source, code) DO NOTHING;
INSERT INTO public.entities (source, code, created_ticks_utc, updated_ticks_utc)
SELECT source, code,
(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000,
(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000
FROM tickers
ON CONFLICT(source, code) DO NOTHING;",
            _ => ThrowSqlNotImplemented()
        };

        public string SetSplitIndicatorForEntities => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
UPDATE entities SET has_splits = false;
UPDATE entities
SET has_splits = true,
updated_ticks_utc = (EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000
FROM splits
WHERE entities.source = splits.source AND entities.code = splits.code;",
            _ => ThrowSqlNotImplemented()
        };

        public string SetPriceActionIndicatorForEntities => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
UPDATE entities SET has_price_actions = false;
UPDATE entities e
SET 
    has_price_actions = true,
    updated_ticks_utc = (EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) * 10000000) + 621355968000000000
FROM (
    SELECT distinct source, code
    FROM eod_adjusted_prices
    UNION
    SELECT distinct source, code
    FROM eod_prices
) AS combined_data
WHERE e.source = combined_data.source AND e.code = combined_data.code;",
            _ => ThrowSqlNotImplemented()
        };

        public string SetLastPriceActionForEntities => _dbDef.Engine switch { 
            DatabaseEngine.PostgreSql => @"
WITH combined_data AS (
SELECT e.source AS e_source,
p.source AS p_source,
e.code AS e_code,
p.code AS p_code,
GREATEST(COALESCE(MAX(e.date_eod), '1900-01-01'), COALESCE(MAX(p.date_eod), '1900-01-01')) AS max_date
FROM (SELECT source, code, MAX(date_eod) AS date_eod FROM eod_adjusted_prices GROUP BY source, code) e
FULL OUTER JOIN (SELECT source, code, MAX(date_eod) AS date_eod FROM eod_prices GROUP BY source, code) p
ON e.source = p.source AND e.code = p.code
GROUP BY e.source, e.code, p.source, p.code
)
UPDATE entities e
SET last_price_action_date = cd.max_date
FROM combined_data cd
WHERE (e.source = cd.e_source OR e.source = cd.p_source) AND (e.code = cd.e_code OR e.code = cd.p_code);",
            _ => ThrowSqlNotImplemented()
        };

        public string UpsertEntity => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.entities (
source, code, type, name,
exchange, country, currency, delisted,
sector, industry,
gic_sector, gic_group, gic_industry, gic_sub_industry,
web_url, phone,
created_ticks_utc, updated_ticks_utc)
VALUES (
@Source, @Code, @Type, @Name,
@Exchange, @Country, @Currency, @Delisted,
@Sector, @Industry,
@GicSector, @GicGroup, @GicIndustry, @GicSubIndustry,
@WebUrl, @Phone,
@CreatedTicksUtc, @UpdatedTicksUtc
)
ON CONFLICT (source, code) DO UPDATE SET
type = EXCLUDED.type,
name = EXCLUDED.name,
exchange = EXCLUDED.exchange,
country = EXCLUDED.country,
currency = EXCLUDED.currency,
delisted = EXCLUDED.delisted,
sector = EXCLUDED.sector,
industry = EXCLUDED.industry,
gic_sector = EXCLUDED.gic_sector,
gic_group = EXCLUDED.gic_group,
gic_industry = EXCLUDED.gic_industry,
gic_sub_industry = EXCLUDED.gic_sub_industry,
web_url = EXCLUDED.web_url,
phone = EXCLUDED.phone,
updated_ticks_utc = EXCLUDED.updated_ticks_utc",
            _ => ThrowSqlNotImplemented()
        };

        public string FetchEntity => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
SELECT source, code, type, name, exchange, country,
currency, delisted, ignored, 
has_splits AS HasSplits,
has_dividends AS HasDividends,
has_price_actions AS HasPriceActions,
has_fundamentals AS HasFundamentals,
last_price_action_date AS LastPriceActionDate,
last_fundamental_date AS LastFundamentalDate,
next_fundamental_date AS NextFundamentalDate,
ignored_reason AS IgnoredReason,
sector, industry,
gic_sector AS GicSector,
gic_group AS GicGroup,
gic_industry AS GicIndustry,
gic_sub_industry AS GicSubIndustry,
web_url AS WebUrl, phone,
created_ticks_utc AS CreatedTicksUtc,
updated_ticks_utc AS UpdatedTicksUtc
FROM public.entities",
            _ => ThrowSqlNotImplemented()
        };

        public string DeleteEntityForSourceAndCode => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.entities
WHERE source = @Source and code = @Code",
            _ => ThrowSqlNotImplemented()
        };
    }
}