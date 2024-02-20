namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    internal class FundamentalsInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string UpsertEntity => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"INSERT INTO public.entities(
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
            DatabaseEngine.PostgreSql => @"SELECT source, code, type, name, exchange, country,
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
FROM public.entities;",
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