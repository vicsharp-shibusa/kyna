using System.Text.Json.Serialization;

namespace Kyna.DataProviders.Polygon.Models;

public struct Split
{
    [JsonPropertyName("execution_date")]
    public DateOnly ExecutionDate;
    [JsonPropertyName("split_from")]
    public double SplitFrom;
    [JsonPropertyName("split_to")]
    public double SplitTo;
    public string Ticker;
}

public struct SplitResponse
{
    public Split[] Results;
    public string Status;
    [JsonPropertyName("request_id")]
    public string RequestId;
    [JsonPropertyName("next_url")]
    public string NextUrl;
}