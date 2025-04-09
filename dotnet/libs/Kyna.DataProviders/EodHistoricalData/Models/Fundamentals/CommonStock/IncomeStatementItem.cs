/*
 * WARNING
 * 
 * The EOD historical data code is no longer supported.
 * This code remains because there is a ton of it, but it is no longer supported.
 * It compiles, of course, but there may be reason to think it will not work as expected.
 * Many changes were made to the system (especially the data access layer), but I was unable
 * to test these changes because I no longer have an active eodhd.com account.
 * I've pretty much switched to using polygon.io.
 */
using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

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
