using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Holding
{
    public string? Code;
    public string? Exchange;
    public string? Name;
    public string? Sector;
    public string? Industry;
    public string? Country;
    public string? Region;
    [JsonPropertyName("assets_%")]
    public double? AssetsPercentage;
}
