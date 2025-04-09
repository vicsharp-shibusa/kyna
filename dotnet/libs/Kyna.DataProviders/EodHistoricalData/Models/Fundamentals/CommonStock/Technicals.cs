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
public struct Technicals
{
    public double? Beta;
    [JsonPropertyName("52WeekHigh")]
    public decimal? FiftyTwoWeekHigh;
    [JsonPropertyName("52WeekLow")]
    public decimal? FiftyTwoWeekLow;
    [JsonPropertyName("50DayMA")]
    public decimal? FiftyDayMovingAverage;
    [JsonPropertyName("200DayMA")]
    public decimal? TwoHundredDayMovingAverage;
    public long? SharesShort;
    public long? SharesShortPriorMonth;
    public double? ShortRatio;
    public double? ShortPercent;
}
