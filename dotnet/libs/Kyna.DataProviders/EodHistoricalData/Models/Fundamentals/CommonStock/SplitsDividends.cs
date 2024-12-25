namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct SplitsDividends
{
    public double? ForwardAnnualDividendRate;
    public double? ForwardAnnualDividendYield;
    public double? PayoutRatio;
    public DateOnly? DividendDate;
    public DateOnly? ExDividendDate;
    public string? LastSplitFactor;
    public DateOnly? LastSplitDate;
    public IDictionary<string, CountForYear>? NumberDividendsByYear;
}
