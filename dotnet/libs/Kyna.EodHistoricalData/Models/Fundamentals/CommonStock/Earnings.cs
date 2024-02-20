namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Earnings
{
    public IDictionary<string, EarningsHistory>? History;
    public IDictionary<string, EarningsTrend>? Trend;
    public IDictionary<string, EarningsPerShare>? Annual;
}
