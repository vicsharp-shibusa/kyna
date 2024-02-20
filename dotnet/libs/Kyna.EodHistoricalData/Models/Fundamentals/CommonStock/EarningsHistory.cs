namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct EarningsHistory
{
    public DateOnly? ReportDate;
    public DateOnly? Date;
    public string? BeforeAfterMarket;
    public string? Currency;
    public decimal? EpsActual;
    public decimal? EpsEstimate;
    public decimal? EpsDifference;
    public double? SurprisePercent;
}
