using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Patterns;

public sealed class CandlestickPattern(PatternName signalName,
    int numberRequired,
    TrendSentiment sentiment,
    TrendSentiment requiredSentiment)
    : OhlcPatternBase(signalName, numberRequired, sentiment, requiredSentiment)
{
    public override required Func<Chart, int, int, double, int> IsMatch { get; init; }
}
