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
namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

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
    public string? Description;
    public string? Category;
    public DateOnly? UpdatedAt;
}