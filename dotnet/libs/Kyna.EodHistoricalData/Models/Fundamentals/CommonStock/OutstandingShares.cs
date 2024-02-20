namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct OutstandingShares
{
    public IDictionary<string, OutstandingSharesItem>? Annual;
    public IDictionary<string, OutstandingSharesItem>? Quarterly;
}
