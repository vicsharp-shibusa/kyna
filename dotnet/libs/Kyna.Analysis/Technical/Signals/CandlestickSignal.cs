namespace Kyna.Analysis.Technical.Signals;

public sealed class CandlestickSignal(SignalName signalName,
    int numberRequired,
    TrendSentiment sentiment,
    TrendSentiment requiredSentiment,
    SignalOptions options) : OhlcSignalBase(signalName, numberRequired, sentiment, requiredSentiment, options)
{
    public override required Func<Chart, int, int, int, double, bool> IsMatch { get; init; }
}
