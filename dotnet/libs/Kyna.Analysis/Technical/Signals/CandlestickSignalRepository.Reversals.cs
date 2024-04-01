namespace Kyna.Analysis.Technical.Signals;

public sealed partial class CandlestickSignalRepository
{
    private static bool PrologueIsBullish(Candlestick candlestick,
        Candlestick[] prologue, TrendSentiment trend) =>
        prologue.All(p => p.High < candlestick.High) && trend == TrendSentiment.Bullish;

    private static bool PrologueIsBearish(Candlestick candlestick,
        Candlestick[] prologue, TrendSentiment trend) =>
        prologue.All(p => p.Low > candlestick.Low) && trend == TrendSentiment.Bearish;

    private static int IsBullishEngulfing(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsBullishEngulfingWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        if (first.IsDark &&
            second.IsLight &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > first.Volume &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment))
        {
            for (int i = position + 2; i < (position + numberRequired); i++)
            {
                var candle = chart.Candlesticks[i];
                if (candle.IsLight &&
                    candle.IsTallBody &&
                    chart.IsTall(i) &&
                    candle.Volume > (chart.Candlesticks[i-1].Volume * volumeFactor))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static int IsBearishEngulfingWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        if (first.IsLight &&
            second.IsDark &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment))
        {
            for (int i = position + 2; i < (position + numberRequired); i++)
            {
                var candle = chart.Candlesticks[i];
                if (candle.IsDark &&
                    candle.IsTallBody &&
                    chart.IsTall(i) &&
                    candle.Volume > (chart.Candlesticks[i - 1].Volume * volumeFactor))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static int IsBearishEngulfing(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    /*
     * This is also known as a "hanging man."
     */
    private static int IsBullishHammer(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return first.IsUmbrella &&
           PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
           ? position : -1;
    }

    private static int IsBearishHammer(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return first.IsUmbrella &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position : -1;
    }

    private static int IsDarkCloudCover(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsPiercing(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsMorningStar(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsEveningStar(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsMorningDojiStar(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsEveningDojiStar(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsShootingStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return (first.IsInvertedUmbrella || first.IsGravestoneDoji) &&
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position : -1;
    }

    private static int IsInvertedHammer(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];

        return (first.IsInvertedUmbrella || first.IsGravestoneDoji) &&
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position : -1;
    }

    private static int IsBullishHarami(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsBearishHarami(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsBullishHaramiCross(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsBearishHaramiCross(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsTweezerTop(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsTweezerBottom(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsBullishBelthold(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position : -1;
    }

    private static int IsBearishBelthold(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position : -1;
    }

    private static int IsUpsideGapTwoCrows(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsThreeBlackCrows(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsThreeWhiteSoldiers(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 2 : -1;
    }

    private static int IsBullishCounterattack(Chart chart,
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
            PrologueIsBearish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }

    private static int IsBearishCounterattack(Chart chart,
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
            PrologueIsBullish(first, prologue, chart.TrendValues[position].Sentiment)
            ? position + 1 : -1;
    }
}
