using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct CashFlowItem
{
    public DateOnly? Date;
    [JsonPropertyName("filing_date")]
    public DateOnly? FilingDate;
    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol;
    public decimal? Investments;
    public decimal? ChangeToLiabilities;
    public decimal? TotalCashflowsFromInvestingActivities;
    public decimal? NetBorrowings;
    public decimal? TotalCashFromFinancingActivities;
    public decimal? ChangeToOperatingActivities;
    public decimal? NetIncome;
    public decimal? ChangeInCash;
    public decimal? BeginPeriodCashFlow;
    public decimal? EndPeriodCashFlow;
    public decimal? TotalCashFromOperatingActivities;
    public decimal? IssuanceOfCapitalStock;
    public decimal? Depreciation;
    public decimal? OtherCashflowsFromInvestingActivities;
    public decimal? DividendsPaid;
    public decimal? ChangeToInventory;
    public decimal? ChangeToAccountReceivables;
    public decimal? SalePurchaseOfStock;
    public decimal? OtherCashflowsFromFinancingActivities;
    public decimal? ChangeToNetincome;
    public decimal? CapitalExpenditures;
    public decimal? ChangeReceivables;
    public decimal? CashFlowsOtherOperating;
    public decimal? ExchangeRateChanges;
    public decimal? CashAndCashEquivalentsChanges;
    public decimal? ChangeInWorkingCapital;
    public decimal? OtherNonCashItems;
    public decimal? FreeCashFlow;
}
