namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct SharesStats
{
    public double? SharesOutstanding;
    public double? SharesFloat;
    public double? PercentInsiders;
    public double? PercentInstitutions;
    public double? SharesShort;
    public double? SharesShortPriorMonth;
    public double? ShortRatio;
    public double? ShortPercentOutstanding;
    public double? ShortPercentFloat;
}
