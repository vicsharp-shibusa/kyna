using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Address
{
    public string? Street;
    public string? City;
    public string? State;
    public string? Country;
    [JsonPropertyName("Zip")]
    public string? PostalCode;
}
