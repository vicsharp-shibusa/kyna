using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models;

public struct Split
{
    public DateOnly Date;

    [JsonPropertyName("split")]
    public string SplitText;
}

