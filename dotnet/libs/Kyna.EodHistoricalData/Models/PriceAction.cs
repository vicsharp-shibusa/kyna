using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models;

public struct PriceAction
{
    public DateOnly Date;
    public decimal Open;
    public decimal High;
    public decimal Low;
    public decimal Close;
    [JsonPropertyName("adjusted_close")]
    public decimal? AdjustedClose;
    public long Volume;
}
