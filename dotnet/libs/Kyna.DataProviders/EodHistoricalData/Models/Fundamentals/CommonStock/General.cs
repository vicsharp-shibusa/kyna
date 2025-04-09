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
public struct General
{
    public string? Code;
    public string? Type;
    public string? Name;
    public string? Exchange;
    public string? CurrencyCode;
    public string? CurrencyName;
    public string? CurrencySymbol;
    public string? CountryName;
    public string? CountryIso;
    public string? Isin;
    public string? Lei;
    public string? Cusip;
    public string? Cik;
    public string? EmployerIdNumber;
    public string? FiscalYearEnd;
    public DateOnly? IPODate;
    public string? InternationalDomestic;
    public string? Sector;
    public string? Industry;
    public string? GicSector;
    public string? GicGroup;
    public string? GicIndustry;
    public string? GicSubIndustry;
    public string? HomeCategory;
    public bool? IsDelisted;
    public string? Description;
    public string? Address;
    public Address? AddressData;
    public string? Phone;
    public string? WebUrl;
    public string? LogoUrl;
    public int? FullTimeEmployees;
    public DateOnly? UpdatedAt;
    [JsonPropertyName("fund_summary")]
    public string? FundSummary;
    [JsonPropertyName("fund_family")]
    public string? FundFamily;
    [JsonPropertyName("fund_category")]
    public string? FundCategory;
    [JsonPropertyName("fund_style")]
    public string? FundStyle;
    public double? MarketCapitalization;
    public IDictionary<string, Officer>? Officers;
    public IDictionary<string, Listing>? Listings;
}