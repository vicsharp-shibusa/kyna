namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetEodPriceSql()
    {
        // Upsert an EOD price record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertEodPrice, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.eod_prices (
    source, code, date_eod, open, high, low, close, volume, 
    created_at, updated_at, process_id)
VALUES (
    @Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, 
    @CreatedAt, @UpdatedAt, @ProcessId)
ON CONFLICT (source, code, date_eod) DO UPDATE
SET 
    open = EXCLUDED.open,
    high = EXCLUDED.high,
    low = EXCLUDED.low,
    close = EXCLUDED.close,
    volume = EXCLUDED.volume,
    updated_at = EXCLUDED.updated_at,
    process_id = EXCLUDED.process_id");

        // Fetch all EOD prices
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchEodPrices, DatabaseEngine.PostgreSql),
            @"
SELECT
    source, code, date_eod AS DateEod, open, high, low, close, volume,
    created_at AS CreatedAt, updated_at AS UpdatedAt,
    process_id AS ProcessId
FROM public.eod_prices");

        // Fetch distinct codes with splits for a source
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchCodesWithSplits, DatabaseEngine.PostgreSql),
            @"
SELECT DISTINCT P.code
FROM public.eod_prices P
JOIN public.splits S ON P.source = S.source AND P.code = S.code
WHERE P.source = @Source
ORDER BY P.code");

        // Copy EOD prices without splits to adjusted prices table
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.CopyPricesWithoutSplitsToAdjustedPrices, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.eod_adjusted_prices (
    source, code, date_eod, open, high, low, close, volume, factor,
    created_at, updated_at, process_id)
SELECT 
    P.source, P.code, P.date_eod, P.open, P.high, P.low, P.close, P.volume, 1,
    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, P.process_id
FROM public.eod_prices P
WHERE NOT EXISTS (
    SELECT 1
    FROM public.splits S
    WHERE S.source = P.source AND S.code = P.code
)
ON CONFLICT (source, code, date_eod) DO UPDATE
SET 
    open = EXCLUDED.open,
    high = EXCLUDED.high,
    low = EXCLUDED.low,
    close = EXCLUDED.close,
    volume = EXCLUDED.volume,
    factor = 1,
    updated_at = CURRENT_TIMESTAMP;");

        // Fetch distinct codes without splits
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchCodesWithoutSplits, DatabaseEngine.PostgreSql),
            @"
SELECT DISTINCT P.code
FROM public.eod_prices P
LEFT JOIN public.splits S ON P.source = S.source AND P.code = S.code
WHERE S.source IS NULL
ORDER BY P.code");

        // Delete all EOD prices (use with caution)
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteEodPrices, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.eod_prices");

        // Delete EOD prices that have corresponding adjusted prices
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteEodPricesWithAdjustedPrices, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.eod_prices
WHERE (source, code, date_eod) IN (
    SELECT source, code, date_eod 
    FROM public.eod_adjusted_prices
)");

        // Upsert an adjusted EOD price record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertAdjustedEodPrice, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.eod_adjusted_prices (
    source, code, date_eod, open, high, low, close, volume, factor,
    created_at, updated_at, process_id)
VALUES (
    @Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, @Factor,
    @CreatedAt, @UpdatedAt, @ProcessId)
ON CONFLICT (source, code, date_eod) DO UPDATE
SET 
    open = EXCLUDED.open,
    high = EXCLUDED.high,
    low = EXCLUDED.low,
    close = EXCLUDED.close,
    volume = EXCLUDED.volume,
    factor = EXCLUDED.factor,
    updated_at = EXCLUDED.updated_at,
    process_id = EXCLUDED.process_id");

        // Fetch all adjusted EOD prices
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchAdjustedEodPrices, DatabaseEngine.PostgreSql),
            @"
SELECT
    source, code, date_eod AS DateEod, open, high, low, close, volume, factor,
    created_at AS CreatedAt, updated_at AS UpdatedAt,
    process_id AS ProcessId
FROM public.eod_adjusted_prices");
    }

    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetAdjustedEodPriceSql()
    {
        // Upsert an adjusted EOD price record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertAdjustedEodPrice, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.eod_adjusted_prices (
    source, code, date_eod, open, high, low, close, volume, factor,
    created_at, updated_at, process_id)
VALUES (
    @Source, @Code, @DateEod, @Open, @High, @Low, @Close, @Volume, @Factor,
    @CreatedAt, @UpdatedAt, @ProcessId)
ON CONFLICT (source, code, date_eod) DO UPDATE
SET 
    open = EXCLUDED.open,
    high = EXCLUDED.high,
    low = EXCLUDED.low,
    close = EXCLUDED.close,
    volume = EXCLUDED.volume,
    factor = EXCLUDED.factor,
    updated_at = EXCLUDED.updated_at,
    process_id = EXCLUDED.process_id");

        // Fetch all adjusted EOD prices
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchAdjustedEodPrices, DatabaseEngine.PostgreSql),
            @"
SELECT
    source, code, date_eod AS DateEod, open, high, low, close, volume, factor,
    created_at AS CreatedAt, updated_at AS UpdatedAt,
    process_id AS ProcessId
FROM public.eod_adjusted_prices");

        // Fetch all distinct adjusted symbols for a source
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchAllAdjustedSymbolsForSource, DatabaseEngine.PostgreSql),
            @"
SELECT DISTINCT code
FROM public.eod_adjusted_prices
WHERE source = @Source
ORDER BY code");

        // Delete all adjusted EOD prices (use with caution)
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteAdjustedEodPrices, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.eod_adjusted_prices");

        // Migrate prices without splits to adjusted prices
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.MigratePricesWithoutSplitsToAdjustedPrices, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.eod_adjusted_prices (
    source, code, date_eod, open, high, low, close, volume, factor,
    created_at, updated_at, process_id)
SELECT 
    P.source, P.code, P.date_eod, P.open, P.high, P.low, P.close, P.volume, 1,
    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, P.process_id
FROM public.eod_prices P
LEFT JOIN public.splits S ON P.source = S.source AND P.code = S.code
WHERE S.source IS NULL
ON CONFLICT (source, code, date_eod) DO UPDATE
SET 
    open = EXCLUDED.open,
    high = EXCLUDED.high,
    low = EXCLUDED.low,
    close = EXCLUDED.close,
    volume = EXCLUDED.volume,
    factor = EXCLUDED.factor,
    updated_at = EXCLUDED.updated_at,
    process_id = EXCLUDED.process_id");

        // Fetch adjusted codes with counts and industry/sector info
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchAdjustedCodesAndCounts, DatabaseEngine.PostgreSql),
            @"
SELECT 
    P.code, E.industry, E.sector, COUNT(*) AS Count
FROM public.eod_adjusted_prices P
JOIN public.entities E ON P.source = E.source AND P.code = E.code
WHERE P.source = @Source
GROUP BY P.code, E.industry, E.sector
HAVING COUNT(*) > 500 AND AVG(P.close) > 15
ORDER BY P.code");

        // Fetch adjusted codes with date ranges
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchAdjustedCodesAndDates, DatabaseEngine.PostgreSql),
            @"
SELECT DISTINCT 
    source, code, MIN(date_eod) AS Start, MAX(date_eod) AS Finish
FROM public.eod_adjusted_prices
GROUP BY source, code
ORDER BY source, code");
    }

    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetSplitsSql()
    {
        // Upsert a split record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new(SqlKeys.UpsertSplit, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.splits (
    source, code, date_split, before_split, after_split, factor,
    created_at, updated_at, process_id)
VALUES (
    @Source, @Code, @SplitDate, @Before, @After, @Factor,
    @CreatedAt, @UpdatedAt, @ProcessId)
ON CONFLICT (source, code, date_split) DO UPDATE
SET 
    before_split = EXCLUDED.before_split,
    after_split = EXCLUDED.after_split,
    factor = EXCLUDED.factor,
    updated_at = EXCLUDED.updated_at,
    process_id = EXCLUDED.process_id");

        // Fetch all splits
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchSplits, DatabaseEngine.PostgreSql),
            @"
SELECT
    source, code, date_split AS SplitDate, before_split AS Before, after_split AS After,
    factor, created_at AS CreatedAt, updated_at AS UpdatedAt, process_id AS ProcessId
FROM public.splits");

        // Delete splits for a specific source
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteSplitsForSource, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.splits 
WHERE source = @Source");
    }

    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetDividendsSql()
    {
        // Upsert a dividend record
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertDividend, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.dividends (
    source, code, type, declaration_date, ex_dividend_date, pay_date,
    record_date, frequency, amount, created_at, updated_at, process_id)
VALUES (
    @Source, @Code, @Type, @DeclarationDate, @ExDividendDate, @PayDate,
    @RecordDate, @Frequency, @Amount, @CreatedAt, @UpdatedAt, @ProcessId)
ON CONFLICT (source, code, type, declaration_date) DO UPDATE
SET
    ex_dividend_date = EXCLUDED.ex_dividend_date,
    pay_date = EXCLUDED.pay_date,
    record_date = EXCLUDED.record_date,
    frequency = EXCLUDED.frequency,
    amount = EXCLUDED.amount,
    updated_at = EXCLUDED.updated_at,
    process_id = EXCLUDED.process_id");

        // Fetch all dividends
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchDividends, DatabaseEngine.PostgreSql),
            @"
SELECT
    source, code, type, declaration_date AS DeclarationDate,
    ex_dividend_date AS ExDividendDate, pay_date AS PayDate,
    record_date AS RecordDate, frequency, amount,
    created_at AS CreatedAt, updated_at AS UpdatedAt, process_id AS ProcessId
FROM public.dividends");

        // Delete dividends for a specific source
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteDividendsForSource, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.dividends 
WHERE source = @Source");
    }

    private static IEnumerable<KeyValuePair<SqlRepoKey, string>> GetFundamentalsSql()
    {
        // Insert a basic entity
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.InsertBasicEntity, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.entities (
    source, code, created_at, updated_at)
VALUES (
    @Source, @Code, @CreatedAt, @UpdatedAt)
ON CONFLICT (source, code) DO NOTHING");

        // Update splits in entities (empty in original, so providing a placeholder)
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpdateSplitsInEntities, DatabaseEngine.PostgreSql),
            @"
-- Placeholder: No operation defined in original source
SELECT 1 WHERE FALSE");

        // Hydrate missing entities
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.HydrateMissingEntities, DatabaseEngine.PostgreSql),
            @"
CREATE TEMPORARY TABLE tickers (
    source TEXT NOT NULL,
    code TEXT NOT NULL,
    PRIMARY KEY (source, code)
);
INSERT INTO tickers
SELECT source, code FROM public.eod_prices
ON CONFLICT (source, code) DO NOTHING;
INSERT INTO tickers
SELECT source, code FROM public.eod_adjusted_prices
ON CONFLICT (source, code) DO NOTHING;
INSERT INTO public.entities (source, code, created_at, updated_at)
SELECT source, code, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM tickers
ON CONFLICT (source, code) DO NOTHING");

        // Set split indicator for entities
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SetSplitIndicatorForEntities, DatabaseEngine.PostgreSql),
            @"
UPDATE public.entities SET has_splits = FALSE;
UPDATE public.entities
SET has_splits = TRUE,
    updated_at = CURRENT_TIMESTAMP
FROM public.splits
WHERE entities.source = splits.source AND entities.code = splits.code");

        // Set price action indicator for entities
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SetPriceActionIndicatorForEntities, DatabaseEngine.PostgreSql),
            @"
UPDATE public.entities SET has_price_actions = FALSE;
UPDATE public.entities e
SET 
    has_price_actions = TRUE,
    updated_at = CURRENT_TIMESTAMP
FROM (
    SELECT DISTINCT source, code
    FROM public.eod_adjusted_prices
    UNION
    SELECT DISTINCT source, code
    FROM public.eod_prices
) AS combined_data
WHERE e.source = combined_data.source AND e.code = combined_data.code");

        // Set last price action for entities
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.SetLastPriceActionForEntities, DatabaseEngine.PostgreSql),
            @"
WITH combined_data AS (
    SELECT 
        e.source AS e_source, p.source AS p_source,
        e.code AS e_code, p.code AS p_code,
        GREATEST(COALESCE(MAX(e.date_eod), '1900-01-01'), COALESCE(MAX(p.date_eod), '1900-01-01')) AS max_date
    FROM (SELECT source, code, MAX(date_eod) AS date_eod FROM public.eod_adjusted_prices GROUP BY source, code) e
    FULL OUTER JOIN (SELECT source, code, MAX(date_eod) AS date_eod FROM public.eod_prices GROUP BY source, code) p
    ON e.source = p.source AND e.code = p.code
    GROUP BY e.source, e.code, p.source, p.code
)
UPDATE public.entities e
SET last_price_action_date = cd.max_date
FROM combined_data cd
WHERE (e.source = cd.e_source OR e.source = cd.p_source) AND (e.code = cd.e_code OR e.code = cd.p_code)");

        // Delete entities without types or price actions
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteEntitiesWithoutTypesOrPriceActions, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.splits
WHERE source = @Source AND code IN (SELECT code FROM public.entities
    WHERE type IS NULL AND source = @Source);
DELETE FROM public.dividends
WHERE source = @Source AND code IN (SELECT code FROM public.entities
    WHERE type IS NULL AND source = @Source);
DELETE FROM public.eod_prices
WHERE source = @Source AND code IN (SELECT code FROM public.entities
    WHERE type IS NULL AND source = @Source);
DELETE FROM public.eod_adjusted_prices
WHERE source = @Source AND code IN (SELECT code FROM public.entities
    WHERE type IS NULL AND source = @Source);
DELETE FROM public.entities 
WHERE type IS NULL AND source = @Source;
DELETE FROM public.entities 
WHERE (source, code) IN (
    SELECT E.source, E.code FROM public.entities E
    LEFT JOIN public.eod_prices P ON E.source = P.source AND E.code = P.code
    WHERE E.source = @Source AND P.code IS NULL)");

        // Upsert an entity
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.UpsertEntity, DatabaseEngine.PostgreSql),
            @"
INSERT INTO public.entities (
    source, code, type, name, exchange, country, currency, delisted,
    sector, industry, gic_sector, gic_group, gic_industry, gic_sub_industry,
    web_url, phone, created_at, updated_at)
VALUES (
    @Source, @Code, @Type, @Name, @Exchange, @Country, @Currency, @Delisted,
    @Sector, @Industry, @GicSector, @GicGroup, @GicIndustry, @GicSubIndustry,
    @WebUrl, @Phone, @CreatedAt, @UpdatedAt)
ON CONFLICT (source, code) DO UPDATE
SET 
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
    updated_at = EXCLUDED.updated_at");

        // Fetch all entities
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.FetchEntity, DatabaseEngine.PostgreSql),
            @"
SELECT 
    source, code, type, name, exchange, country, currency, delisted, ignored,
    has_splits AS HasSplits, has_dividends AS HasDividends, has_price_actions AS HasPriceActions,
    has_fundamentals AS HasFundamentals, last_price_action_date AS LastPriceActionDate,
    last_fundamental_date AS LastFundamentalDate, next_fundamental_date AS NextFundamentalDate,
    ignored_reason AS IgnoredReason, sector, industry,
    gic_sector AS GicSector, gic_group AS GicGroup, gic_industry AS GicIndustry,
    gic_sub_industry AS GicSubIndustry, web_url AS WebUrl, phone,
    created_at AS CreatedAt, updated_at AS UpdatedAt
FROM public.entities");

        // Delete entity for a specific source and code
        yield return new KeyValuePair<SqlRepoKey, string>(
            new SqlRepoKey(SqlKeys.DeleteEntityForSourceAndCode, DatabaseEngine.PostgreSql),
            @"
DELETE FROM public.entities
WHERE source = @Source AND code = @Code");
    }
}