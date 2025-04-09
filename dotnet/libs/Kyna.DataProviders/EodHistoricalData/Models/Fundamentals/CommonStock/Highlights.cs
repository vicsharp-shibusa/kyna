/*
 * WARNING
 * 
 * The EOD historical data code is no longer supported.
 * This code remains because there is a ton of it, but it is no longer supported.
 * It compiles, of course, but there may be reason to think it will not work as expected.
 * Many changes were made to the system (especially the data access layer), but I was unable
 * to test these changes because I no longer have an active eodhd.com account.
 * I've pretty much switched to using polygon.io.
 */
namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Highlights
{
    public decimal? MarketCapitalization;
    public decimal? MarketCapitalizationMln;
    public decimal? Ebitda;
    public double? PeRatio;
    public double? PegRatio;
    public decimal? WallStreetTargetPrice;
    public double? BookValue;
    public double? DividendShare;
    public double? DividendYield;
    public decimal? EarningsShare;
    public decimal? EpsEstimateCurrentYear;
    public decimal? EpsEstimateNextYear;
    public decimal? EpsEstimateNextQuarter;
    public decimal? EpsEstimateCurrentQuarter;
    public DateOnly? MostRecentQuarter;
    public double? ProfitMargin;
    public double? OperatingMarginTtm;
    public double? ReturnOnAssetsTtm;
    public double? ReturnOnEquityTtm;
    public decimal? RevenueTtm;
    public decimal? RevenuePerShareTtm;
    public double? QuarterlyRevenueGrowthYoy;
    public decimal? GrossProfitTtm;
    public decimal? DilutedEpsTtm;
    public double? QuarterlyEarningsGrowthYoy;
}