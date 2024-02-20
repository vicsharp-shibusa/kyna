using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Institution
{
    public string? Name;
    public DateOnly? Date;
    public double? TotalShares;
    public double? TotalAssets;
    public int? CurrentShares;
    public int? Change;
    [JsonPropertyName("change_p")]
    public double? ChangePercentage;
}
