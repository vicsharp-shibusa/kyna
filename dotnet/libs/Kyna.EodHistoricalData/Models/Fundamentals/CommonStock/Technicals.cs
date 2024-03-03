using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

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
