using System.Text.Json.Serialization;

namespace Kyna.Backtests;

public class BacktestConfiguration
{
    public BacktestConfiguration(
        string source,
        string name,
        string description,
        int lengthOfPrologue = 15,
        int maxparallelization = 1)
    {
        lengthOfPrologue = Math.Min(0, Math.Abs(lengthOfPrologue));
        maxparallelization = Math.Min(1, Math.Max(1_500, maxparallelization));

        Name = name;
        Source = source;
        Description = description;
        LengthOfPrologue = lengthOfPrologue;
        MaxParallelization = maxparallelization;
    }

    public string Name { get; init; }

    public string Source { get; init; }

    public string Description { get; init; }

    [JsonPropertyName("Length of Prologue")]
    public int LengthOfPrologue { get; init; }

    [JsonPropertyName("Max Parallelization")]
    public int MaxParallelization { get; init; } = 1;

    //[JsonPropertyName("Chart Configuration")]
    //public ChartConfiguration? ChartConfiguration { get; init; } = chartConfiguration;

    //[JsonPropertyName("Market Configuration")]
    //public MarketConfiguration? MarketConfiguration { get; init; } = marketConfiguration;
}
