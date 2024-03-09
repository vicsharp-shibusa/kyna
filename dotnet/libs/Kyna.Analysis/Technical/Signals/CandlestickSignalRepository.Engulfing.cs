namespace Kyna.Analysis.Technical.Signals;

public sealed partial class CandlestickSignalRepository
{
    private static bool IsBullishEngulfing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsDown &&
            second.IsUp &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > first.Volume &&
            prologue.All(p => p.Low > first.Low) &&
            chart.TrendValues[position].Sentiment == TrendSentiment.Bearish;
    }

    private static bool IsBearishEngulfing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsUp &&
                second.IsDown &&
                second.Body.Low < first.Body.Low &&
                second.Body.High > first.Body.High &&
                second.Volume > first.Volume &&
                prologue.All(p => p.High < first.High) &&
                chart.TrendValues[position].Sentiment == TrendSentiment.Bullish;
    }
}
