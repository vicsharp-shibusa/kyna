using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models;

public struct Split
{
    public DateOnly Date;
    [JsonPropertyName("split")]
    public string SplitText;
}

