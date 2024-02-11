using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData;

public struct Split
{
    public DateOnly Date;

    [JsonPropertyName("split")]
    public string SplitText;
}

