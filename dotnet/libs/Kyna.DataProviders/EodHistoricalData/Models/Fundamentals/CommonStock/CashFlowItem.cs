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
