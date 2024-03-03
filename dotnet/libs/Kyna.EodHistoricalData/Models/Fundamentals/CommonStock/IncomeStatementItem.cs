using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct IncomeStatementItem
{
    public DateOnly? Date;
    [JsonPropertyName("filing_date")]
    public DateOnly? FilingDate;
    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol;
    public decimal? ResearchDevelopment;
    public decimal? EffectOfAccountingCharges;
    public decimal? IncomeBeforeTax;
    public decimal? MinorityInterest;
    public decimal? NetIncome;
    public decimal? SellingGeneralAdministrative;
    public decimal? SellingAndMarketingExpenses;
    public decimal? GrossProfit;
    public decimal? ReconciledDepreciation;
    public decimal? Ebit;
    public decimal? Ebitda;
    public decimal? DepreciationAndAmortization;
    public decimal? NonOperatingIncomeNetOther;
    public decimal? OperatingIncome;
    public decimal? OtherOperatingExpenses;
    public decimal? InterestExpense;
    public decimal? TaxProvision;
    public decimal? InterestIncome;
    public decimal? NetInterestIncome;
    public decimal? ExtraordinaryItems;
    public decimal? NonRecurring;
    public decimal? OtherItems;
    public decimal? IncomeTaxExpense;
    public decimal? TotalRevenue;
    public decimal? TotalOperatingExpenses;
    public decimal? CostOfRevenue;
    public decimal? TotalOtherIncomeExpenseNet;
    public decimal? DiscontinuedOperations;
    public decimal? NetIncomeFromContinuingOps;
    public decimal? NetIncomeApplicableToCommonShares;
    public decimal? PreferredStockAndOtherAdjustments;
}
