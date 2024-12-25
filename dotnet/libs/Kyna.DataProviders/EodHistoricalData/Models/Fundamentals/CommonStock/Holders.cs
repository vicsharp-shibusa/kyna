namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Holders
{
    public IDictionary<string, Institution>? Institutions;
    public IDictionary<string, Institution>? Funds;
}
