using Kyna.Common;

namespace Kyna.Analysis.Technical.Signals;

public abstract class OhlcSignalBase(
    SignalName signalName,
    int numberRequired,
    TrendSentiment sentiment,
    TrendSentiment requiredSentiment,
    SignalOptions options)
{
    public SignalOptions Options { get; init; } = options;
    public SignalName SignalName { get; init; } = signalName;

    /// <summary>
    /// Represents the number of OHLC or candles required to form the signal.
    /// </summary>
    public int NumberRequired { get; init; } = numberRequired;

    /// <summary>
    /// Represents the expected sentiment of the signal, or
    /// which way the security should go after the occurrence of the signal.
    /// </summary>
    public TrendSentiment Sentiment { get; init; } = sentiment;

    /// <summary>
    /// Represents the sentiment the security should be in
    /// for this signal to be effective.
    /// </summary>
    public TrendSentiment RequiredSentiment { get; init; } = requiredSentiment;

    /// <summary>
    /// Represents a function to determine if a given position on a chart
    /// is a match for a specified signal.
    /// The arguments are Chart, position in chart, number of OHLC required, and
    /// length of prologue.
    /// </summary>
    public abstract Func<Chart, int, int, int, bool> IsMatch { get; init; }

    public virtual IEnumerable<SignalMatch> DiscoverMatches(Chart chart, Chart? market = null, bool signalOnlyWithMarket = false)
    {
        if (chart.Length >= (NumberRequired + Options.LengthOfPrologue))
        {
            bool useMarket = signalOnlyWithMarket && market != null && market.PriceActions.Length > 0;

            for (int i = Options.LengthOfPrologue; i < chart.Length - NumberRequired; i++)
            {
                if (useMarket)
                {
                    var index = market!.GetIndexOfDate(chart.PriceActions[i].Date);
                    if (index.HasValue && market.TrendValues[index.Value].Sentiment != Sentiment)
                    {
                        continue;
                    }
                }
                if (IsMatch(chart, i, NumberRequired, Options.LengthOfPrologue))
                {
                    yield return new SignalMatch()
                    {
                        SignalName = SignalName.GetEnumDescription(),
                        Code = chart.Code ?? "None",
                        Prologue = new ChartRange(i - Options.LengthOfPrologue, i - 1),
                        Signal = new ChartRange(i, i + NumberRequired - 1)
                    };
                }
            }
        }
    }
}

public struct SignalMatch(string signalName, string code, ChartRange prologue, ChartRange signal)
{
    public string SignalName = signalName;
    public string Code = code;
    public ChartRange Prologue = prologue;
    public ChartRange Signal = signal;
}

public struct ChartRange(int start, int end)
{
    public int Start = start;
    public int End = end;
}

public struct SignalOptions(int lengthOfPrologue)
{
    public int LengthOfPrologue = lengthOfPrologue;
}
