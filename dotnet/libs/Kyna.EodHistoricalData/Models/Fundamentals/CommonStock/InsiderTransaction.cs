namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct InsiderTransaction
{
    public DateOnly? Date;
    public string? OwnerCik;
    public string? OwnerName;
    public DateOnly? TransactionDate;
    public string? TransactionCode;
    public int? TransactionAmount;
    public decimal? TransactionPrice;
    public string? TransactionAcquiredDisposed;
    public int? PostTransactionAmount;
    public string? SecLink;
}
