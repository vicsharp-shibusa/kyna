using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Performance
{
    [JsonPropertyName("1y_volatility")]
    public string? OneYearVolatility;
    [JsonPropertyName("3y_volatility")]
    public string? ThreeYearVolatility;
    [JsonPropertyName("3y_expreturn")]
    public string? ThreeYearExpectedReturn;
    [JsonPropertyName("3y_sharpratio")]
    public string? ThreeYearSharpRatio;
    [JsonPropertyName("returns_ytd")]
    public string? ReturnsYearToDate;
    [JsonPropertyName("returns_1y")]
    public string? ReturnsOneYear;
    [JsonPropertyName("returns_3y")]
    public string? ReturnsThreeYear;
    [JsonPropertyName("returns_5y")]
    public string? ReturnsFiveYear;
    [JsonPropertyName("returns_10y")]
    public string? ReturnsTenYear;
}
