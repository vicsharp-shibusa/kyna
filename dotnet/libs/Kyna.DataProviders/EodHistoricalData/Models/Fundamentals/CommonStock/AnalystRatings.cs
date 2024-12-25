namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct AnalystRatings
{
    public double? Rating;
    public decimal? TargetPrice;
    public int? StrongBuy;
    public int? Buy;
    public int? Hold;
    public int? Sell;
    public int? StrongSell;
}
