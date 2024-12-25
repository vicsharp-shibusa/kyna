namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct EarningsTrend
{
    public DateOnly? Date;
    public string? Period;
    public double? Growth;
    public decimal? EarningsEstimateAvg;
    public decimal? EarningsEstimateLow;
    public decimal? EarningsEstimateHigh;
    public decimal? EarningsEstimateYearAgoEps;
    public decimal? EarningsEstimateNumberOfAnalysts;
    public decimal? EarningsEstimateGrowth;
    public decimal? RevenueEstimateAvg;
    public decimal? RevenueEstimateLow;
    public decimal? RevenueEstimateHigh;
    public decimal? RevenueEstimateYearAgoEps;
    public decimal? RevenueEstimateNumberOfAnalysts;
    public decimal? RevenueEstimateGrowth;
    public decimal? EpsTrendCurrent;
    public decimal? EpsTrend7DaysAgo;
    public decimal? EpsTrend30DaysAgo;
    public decimal? EpsTrend60DaysAgo;
    public decimal? EpsTrend90DaysAgo;
    public decimal? EpsRevisionsUpLast7Days;
    public decimal? EpsRevisionsUpLast30Days;
    public decimal? EpsRevisionsDownLast7Days;
    public decimal? EpsRevisionsDownLast30Days;
}
