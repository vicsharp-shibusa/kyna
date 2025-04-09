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
public struct BalanceSheetItem
{
    public DateOnly? Date;
    [JsonPropertyName("filing_date")]
    public DateOnly? FilingDate;
    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol;
    public decimal? TotalAssets;
    public decimal? IntangibleAssets;
    public decimal? EarningAssets;
    public decimal? OtherCurrentAssets;
    public decimal? TotalLiab;
    public decimal? TotalStockholderEquity;
    public decimal? DeferredLongTermLiab;
    public decimal? OtherCurrentLiab;
    public decimal? CommonStock;
    public decimal? CapitalStock;
    public decimal? RetainedEarnings;
    public decimal? OtherLiab;
    public decimal? GoodWill;
    public decimal? OtherAssets;
    public decimal? Cash;
    public decimal? CashAndEquivalents;
    public decimal? TotalCurrentLiabilities;
    public decimal? CurrentDeferredRevenue;
    public decimal? NetDebt;
    public decimal? ShortTermDebt;
    public decimal? ShortLongTermDebt;
    public decimal? ShortLongTermDebtTotal;
    public decimal? OtherStockholderEquity;
    public decimal? PropertyPlantEquipment;
    public decimal? TotalCurrentAssets;
    public decimal? LongTermInvestments;
    public decimal? NetTangibleAssets;
    public decimal? ShortTermInvestments;
    public decimal? NetReceivables;
    public decimal? LongTermDebt;
    public decimal? Inventory;
    public decimal? AccountsPayable;
    public decimal? TotalPermanentEquity;
    public decimal? NoncontrollingInterestInConsolidatedEntity;
    public decimal? TemporaryEquityRedeemableNoncontrollingInterests;
    public decimal? AccumulatedOtherComprehensiveIncome;
    public decimal? AdditionalPaidInCapital;
    public decimal? CommonStockTotalEquity;
    public decimal? PreferredStockTotalEquity;
    public decimal? RetainedEarningsTotalEquity;
    public decimal? TreasuryStock;
    public decimal? AccumulatedAmortization;
    public decimal? NonCurrrentAssetsOther;
    public decimal? DeferredLongTermAssetCharges;
    public decimal? NonCurrentAssetsTotal;
    public decimal? CapitalLeaseObligations;
    public decimal? LongTermDebtTotal;
    public decimal? NonCurrentLiabilitiesOther;
    public decimal? NonCurrentLiabilitiesTotal;
    public decimal? NegativeGoodwill;
    public decimal? Warrants;
    public decimal? PreferredStockRedeemable;
    public decimal? CapitalSurpluse;
    public decimal? LiabilitiesAndStockholdersEquity;
    public decimal? CashAndShortTermInvestments;
    public decimal? PropertyPlantAndEquipmentGross;
    public decimal? PropertyPlantAndEquipmentNet;
    public decimal? AccumulatedDepreciation;
    public decimal? NetWorkingCapital;
    public decimal? NetInvestedCapital;
    public decimal? CommonStockSharesOutstanding;
}