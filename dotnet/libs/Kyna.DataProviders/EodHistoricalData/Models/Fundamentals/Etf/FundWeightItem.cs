using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct FundWeightItem
{
    [JsonPropertyName("fund_%")]
    public string? FundPercentage;
    [JsonPropertyName("relative_to_category")]
    public string? RelativeToCategory;
}
