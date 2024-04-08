using Kyna.Analysis.Technical;
using System.Text.Json.Serialization;

namespace Kyna.Backtests;

public class BacktestingConfiguration(BacktestType type,
    string source,
    string name, 
    string description,
    PricePoint entryPricePoint,
    TestTargetPercentage targetUp,
    TestTargetPercentage targetDown,
    string[]? signalnames = null,
    double volumeFactor = 1D,
    int? maxparallelization = null,
    bool onlySignalWithMarket = false,
    ChartConfiguration? chartConfiguration = null,
    MarketConfiguration? marketConfiguration = null)
{
    public BacktestType Type { get; init; } = type;
    
    public string Name { get; init; } = name;
    
    public string Source { get; init; } = source;
    
    public string Description { get; init; } = description;
    
    [JsonPropertyName("Entry Price Point")]
    public PricePoint EntryPricePoint { get; init; } = entryPricePoint;
    
    [JsonPropertyName("Target Up")]
    public TestTargetPercentage TargetUp { get; init; } = targetUp;
    
    [JsonPropertyName("Target Down")]
    public TestTargetPercentage TargetDown { get; init; } = targetDown;
    
    [JsonPropertyName("Signal Names")]
    public string[]? SignalNames { get; init; } = signalnames;
    
    [JsonPropertyName("Max Parallelization")]
    public int? MaxParallelization { get; init; } = maxparallelization;

    [JsonPropertyName("Only Signal With Market")]
    public bool OnlySignalWithMarket { get; init; } = onlySignalWithMarket;

    [JsonPropertyName("Volume Factor")]
    public double VolumeFactor { get; init; } = volumeFactor;

    [JsonPropertyName("Chart Configuration")]
    public ChartConfiguration? ChartConfiguration { get; init; } = chartConfiguration;

    [JsonPropertyName("Market Configuration")]
    public MarketConfiguration? MarketConfiguration { get; init; } = marketConfiguration;
}
