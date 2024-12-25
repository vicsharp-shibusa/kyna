using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct MorningStar
{
    public string Ratio;
    [JsonPropertyName("category_benchmark")]
    public string? CategoryBenchmark;
    [JsonPropertyName("sustainability_ratio")]
    public string? SustainabilityRatio;
}
