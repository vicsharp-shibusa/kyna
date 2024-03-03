using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct EquityWeightItem
{
    [JsonPropertyName("equity_%")]
    public string? EquityPercentage;
    [JsonPropertyName("relative_to_category")]
    public string? RelativeToCategory;
}
