namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Valuation
{
    public double? TrailingPe;
    public double? ForwardPe;
    public double? PriceSalesTtm;
    public double? PriceBookMrq;
    public long? EnterpriseValue;
    public double? EnterpriseValueRevenue;
    public double? EnterpriseValueEbitda;
}
