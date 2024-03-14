using Kyna.Analysis.Technical;

namespace Kyna.Backtests;

public class BacktestingConfiguration(BacktestType type, string source,
    string name, string description,
    PricePoint entryPricePoint,
    TestTargetPercentage targetUp,
    TestTargetPercentage targetDown,
    string[]? signalnames = null,
    int lengthOfPrologue = 15,
    int? maxparallelization = null)
{
    public BacktestType Type { get; init; } = type;
    public string Name { get; init; } = name;
    public string Source { get; init; } = source;
    public string Description { get; init; } = description;
    public PricePoint EntryPricePoint { get; init; } = entryPricePoint;
    public TestTargetPercentage TargetUp { get; init; } = targetUp;
    public TestTargetPercentage TargetDown { get; init; } = targetDown;
    public string[]? SignalNames { get; init; } = signalnames;
    public int LengthOfPrologue { get; init; } = lengthOfPrologue;
    public int? MaxParallelization { get; init; } = maxparallelization;
}
