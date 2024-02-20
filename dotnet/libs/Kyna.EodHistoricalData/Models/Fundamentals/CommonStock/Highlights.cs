namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

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