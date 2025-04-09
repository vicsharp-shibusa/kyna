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
namespace Kyna.DataProviders.EodHistoricalData.Models;

public struct Symbol
{
    public string? Code;
    public string? Name;
    public string? Country;
    public string? Exchange;
    public string? Currency;
    public string? Type;

    public override readonly string ToString()
    {
        string val = $"{Code} {Exchange} {Type}";
        while (val.Contains("  "))
        {
            val = val.Replace("  ", " ").Trim();
        }
        return string.IsNullOrWhiteSpace(val) ? "" : val;
    }
}
