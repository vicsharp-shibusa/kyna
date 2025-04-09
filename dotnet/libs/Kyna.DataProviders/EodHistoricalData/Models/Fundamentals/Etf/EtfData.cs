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

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct EtfData
{
    public string? Isin;
    [JsonPropertyName("company_name")]
    public string? CompanyName;
    [JsonPropertyName("etf_url")]
    public string? Url;
    public string? Domicile;
    [JsonPropertyName("index_name")]
    public string? IndexName;
    public double? Yield;
    [JsonPropertyName("dividend_paying_frequency")]
    public string? DividendPayingFrequency;
    [JsonPropertyName("inception_date")]
    public DateOnly? InceptionDate;
    [JsonPropertyName("max_annual_mgmt_charge")]
    public string? MaxAnnualManagementCharge;
    [JsonPropertyName("ongoing_charge")]
    public string? OngoingCharge;
    [JsonPropertyName("date_ongoing_charge")]
    public DateOnly? DateOngoingCharge;
    public string? NetExpenseRatio;
    public string? AnnualHoldingsTurnover;
    public string? TotalAssets;
    [JsonPropertyName("average_mkt_cap_mil")]
    public string? AverageMarketCapitalizationMillions;
    [JsonPropertyName("market_capitalisation")]
    public MarketCapitalization MarketCapitalization;
    [JsonPropertyName("asset_allocation")]
    public IDictionary<string, AssetAllocationItem>? AssetAllocations;
    [JsonPropertyName("world_regions")]
    public IDictionary<string, EquityWeightItem>? WorldRegions;
    [JsonPropertyName("sector_weights")]
    public IDictionary<string, EquityWeightItem>? SectorWeights;
    [JsonPropertyName("fixed_income")]
    public IDictionary<string, FundWeightItem>? FixedIncome;
    [JsonPropertyName("holdings_count")]
    public int? HoldingsCount;
    [JsonPropertyName("top_10_holdings")]
    public IDictionary<string, Holding>? TopTenHoldings;
    public IDictionary<string, Holding>? Holdings;
    [JsonPropertyName("valuations_growth")]
    public IDictionary<string, Valuation>? ValuationsGrowth;
    public MorningStar MorningStar;
    public Performance Performance;
}
