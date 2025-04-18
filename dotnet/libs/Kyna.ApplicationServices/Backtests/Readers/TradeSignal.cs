using Kyna.Analysis.Technical;

namespace Kyna.ApplicationServices.Backtests.Readers;

internal readonly struct TradeSignal
{
    public string Code { get; init; }
    public DateOnly Date { get; init; }
    public PatternName SignalName { get; init; }
    public TrendSentiment Sentiment { get; init; }
    public double TrendValue { get; init; }
    public decimal ClosePrice { get; init; }
}
