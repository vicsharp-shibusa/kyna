namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct OutstandingSharesItem
{
    public string? Date;
    public DateOnly? DateFormatted;
    public string? SharesMln;
    public double? Shares; // This is sometimes returned as a non-integer (it has a decimal point)
}
