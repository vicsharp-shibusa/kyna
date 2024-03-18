namespace Kyna.Analysis.Technical.Signals;

public sealed partial class CandlestickSignalRepository
{
    private static bool PrologueIsBullish(Candlestick candlestick,
        Candlestick[] prologue, TrendSentiment trend) =>
        prologue.All(p => p.High < candlestick.High) && trend == TrendSentiment.Bullish;

    private static bool PrologueIsBearish(Candlestick candlestick,
        Candlestick[] prologue, TrendSentiment trend) =>
        prologue.All(p => p.Low > candlestick.Low) && trend == TrendSentiment.Bearish;

    private static bool IsBullishEngulfing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsDark &&
            second.IsLight &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > (first.Volume * volumeFactor) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBearishEngulfing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsLight &&
            second.IsDark &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > (first.Volume * volumeFactor) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    /*
     * This is also known as a "hanging man."
     */
    private static bool IsBullishHammer(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return first.IsUmbrella &&
           PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBearishHammer(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return first.IsUmbrella &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsDarkCloudCover(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsLight &&
            second.IsDark &&
            second.Body.High > first.High &&
            second.Body.Low < first.Body.MidPoint &&
            second.Body.Low >= first.Body.Low &&
            first.Body.Length > prologue.Select(p => p.Body.Length).Average() &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsPiercing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsDark &&
            second.IsLight &&
            second.Body.Low < first.Low &&
            second.Body.High > first.Body.MidPoint &&
            second.Body.High <= first.Body.High &&
            first.Body.Length > prologue.Select(p => p.Body.Length).Average() &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsMorningStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsDark &&
            first.Body.Length > prologue.Select(p => p.Body.Length).Average() &&
            second.Body.High < first.Body.Low &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            third.IsLight &&
            third.Low > second.Low &&
            third.Close > first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsEveningStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsLight &&
            first.Body.Length > prologue.Select(p => p.Body.Length).Average() &&
            second.Body.Low > first.Body.High &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            third.IsDark &&
            third.High < second.High &&
            third.Close < first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsMorningDojiStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsDark &&
            first.Body.Length > prologue.Select(p => p.Body.Length).Average() &&
            second.Body.High < first.Body.Low &&
            chart.IsTall(position) &&
            second.IsDoji &&
            third.IsLight &&
            third.Low > second.Low &&
            third.Close > first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsEveningDojiStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsLight &&
            first.Body.Length > prologue.Select(p => p.Body.Length).Average() &&
            second.Body.Low > first.Body.High &&
            chart.IsTall(position) &&
            second.IsDoji &&
            third.IsDark &&
            third.High < second.High &&
            third.Close < first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsShootingStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return (first.IsInvertedUmbrella || first.IsGravestoneDoji) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsInvertedHammer(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return (first.IsInvertedUmbrella || first.IsGravestoneDoji) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBullishHarami(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.IsTall(position, 0, 1.2M) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBearishHarami(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.IsTall(position, 0, 1.2M) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBullishHaramiCross(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.IsTall(position, 0, 1.2M) &&
            second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBearishHaramiCross(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.IsTall(position, 0, 1.2M) &&
            second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsTweezerTop(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.High == second.High &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsTweezerBottom(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.Low == second.Low &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBullishBelthold(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return first.IsBullishBelthold &&
            chart.IsTall(position) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBearishBelthold(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return first.IsBearishBelthold &&
            chart.IsTall(position) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsUpsideGapTwoCrows(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsLight &&
            second.IsDark &&
            third.IsDark &&
            chart.IsTall(position) &&
            second.Body.Low > first.Body.High &&
            third.Body.High > second.Body.High &&
            third.Body.Low < first.Body.High &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsThreeBlackCrows(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsDark &&
            second.IsDark &&
            third.IsDark &&
            second.Body.High < first.Body.High &&
            second.Body.High > first.Body.Low &&
            third.Body.High < second.Body.High &&
            third.Body.High > second.Body.Low &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsThreeWhiteSoldiers(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return first.IsLight &&
            second.IsLight &&
            third.IsLight &&
            second.Body.Low > first.Body.Low &&
            second.Body.Low < first.Body.High &&
            third.Body.Low > second.Body.Low &&
            third.Body.Low < second.Body.High &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBullishCounterattack(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsDark &&
            chart.IsTall(position) &&
            second.IsLight &&
            first.Close == second.Close &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment);
    }

    private static bool IsBearishCounterattack(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return first.IsLight &&
            chart.IsTall(position) &&
            second.IsDark &&
            first.Close == second.Close &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment);
    }
}
