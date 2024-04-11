namespace Kyna.Analysis.Technical.Signals;

public sealed partial class CandlestickSignalRepository
{
    private static int IsBullishEngulfing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            second.IsLight &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > (first.Volume * volumeFactor)
            ? position + 1 : -1;
    }

    private static int IsBullishEngulfingWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        if (chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            second.IsLight &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > first.Volume)
        {
            for (int i = position + 2; i < (position + numberRequired); i++)
            {
                var candle = chart.Candlesticks[i];
                if (candle.IsLight &&
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

    private static int IsBullishEngulfingWithTallCandles(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            chart.IsTall(position) &&
            second.IsLight &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > (first.Volume * volumeFactor)
            ? position + 1 : -1;
    }

    private static int IsBullishEngulfingWithFourBlackPredecessors(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];
        var fourth = chart.Candlesticks[position + 3];
        var fifth = chart.Candlesticks[position + 4];
        var sixth = chart.Candlesticks[position + 5];

        if (chart.TrendValues[position + 4].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position + 4) == TrendSentiment.Bearish &&
            first.IsDark && second.IsDark && third.IsDark && fourth.IsDark &&
            fifth.IsDark &&
            sixth.IsLight &&
            sixth.Body.Low < fifth.Body.Low &&
            sixth.Body.High > fifth.Body.High &&
            sixth.Volume > (fifth.Volume * volumeFactor))
        {
            return position + 5;
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

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            second.IsDark &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > (first.Volume * volumeFactor)
            ? position + 1 : -1;
    }

    private static int IsBearishEngulfingWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        if (chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            second.IsDark &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High)
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

    private static int IsBearishEngulfingWithTallCandles(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            chart.IsTall(position) &&
            second.IsDark &&
            second.Body.Low < first.Body.Low &&
            second.Body.High > first.Body.High &&
            second.Volume > (first.Volume * volumeFactor)
            ? position + 1 : -1;
    }

    private static int IsBearishEngulfingWithFourWhitePredecessors(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];
        var fourth = chart.Candlesticks[position + 3];
        var fifth = chart.Candlesticks[position + 4];
        var sixth = chart.Candlesticks[position + 5];

        return chart.TrendValues[position + 4].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position + 4) == TrendSentiment.Bullish &&
            first.IsLight && second.IsLight && third.IsLight && fourth.IsLight &&
            fifth.IsLight &&
            sixth.IsDark &&
            sixth.Body.Low < fifth.Body.Low &&
            sixth.Body.High > fifth.Body.High &&
            sixth.Volume > (fifth.Volume * volumeFactor)
            ? position + 5 : -1;
    }

    private static int IsBullishHammer(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        return first.IsUmbrella &&
            chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish
            ? position : -1;
    }

    private static int IsBullishHammerWithFollowThru(Chart chart,
      int position,
      int numberRequired,
      int lengthOfPrologue,
      double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        if (first.IsUmbrella &&
            chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish)
        {
            for (int i = position + 1; i < (position + numberRequired); i++)
            {
                var candle = chart.Candlesticks[i];
                if (candle.IsLight &&
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

    /*
     * This is also known as a "hanging man."
     */
    private static int IsBearishHammer(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        return first.IsUmbrella &&
            chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish
            ? position : -1;
    }

    private static int IsBearishHammerWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        if (first.IsUmbrella &&
            chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish)
        {
            for (int i = position + 1; i < (position + numberRequired); i++)
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

    private static int IsDarkCloudCover(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            chart.IsTall(position) &&
            second.IsDark &&
            second.Body.High > first.High &&
            second.Body.Low < first.Body.MidPoint &&
            second.Body.Low >= first.Body.Low
            ? position + 1 : -1;
    }

    private static int IsDarkCloudCoverWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            chart.IsTall(position) &&
            second.IsDark &&
            second.Body.High > first.High &&
            second.Body.Low < first.Body.MidPoint &&
            second.Body.Low >= first.Body.Low &&
            third.Close < second.Close
            ? position + 2 : -1;
    }

    private static int IsPiercing(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            chart.IsTall(position) &&
            second.IsLight &&
            second.Body.Low < first.Low &&
            second.Body.High > first.Body.MidPoint &&
            second.Body.High <= first.Body.High
            ? position + 1 : -1;
    }

    private static int IsPiercingWithFollowThru(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            chart.IsTall(position) &&
            second.IsLight &&
            second.Body.Low < first.Low &&
            second.Body.High > first.Body.MidPoint &&
            second.Body.High <= first.Body.High &&
            third.Close > second.Close
            ? position + 2 : -1;
    }

    private static int IsMorningStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            second.Body.High < first.Body.Low &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            third.IsLight &&
            third.Low > second.Low &&
            third.Close > first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor)
            ? position + 2 : -1;
    }

    private static int IsEveningStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            second.Body.Low > first.Body.High &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            third.IsDark &&
            third.High < second.High &&
            third.Close < first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor)
            ? position + 2 : -1;
    }

    private static int IsMorningDojiStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            chart.IsTall(position) &&
            second.Body.High < first.Body.Low &&
            second.IsDoji &&
            !second.IsFourPriceDoji &&
            third.IsLight &&
            third.Low > second.Low &&
            third.Close > first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor)
            ? position + 2 : -1;
    }

    private static int IsEveningDojiStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            second.Body.Low > first.Body.High &&
            chart.IsTall(position) &&
            second.IsDoji &&
            !second.IsFourPriceDoji &&
            third.IsDark &&
            third.High < second.High &&
            third.Close < first.Body.MidPoint &&
            third.Volume > (first.Volume * volumeFactor) &&
            third.Volume > (second.Volume * volumeFactor)
            ? position + 2 : -1;
    }

    private static int IsShootingStar(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            (first.IsInvertedUmbrella || first.IsGravestoneDoji)
            ? position : -1;
    }

    private static int IsInvertedHammer(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            (first.IsInvertedUmbrella || first.IsGravestoneDoji)
            ? position : -1;
    }

    private static int IsBullishHarami(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            chart.IsTall(position, 0, 1.2M) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low
            ? position + 1 : -1;
    }

    private static int IsBearishHarami(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            chart.IsTall(position, 0, 1.2M) &&
            chart.IsShort(position + 1) &&
            !second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low
            ? position + 1 : -1;
    }

    private static int IsBullishHaramiCross(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            chart.IsTall(position, 0, 1.2M) &&
            second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low
            ? position + 1 : -1;
    }

    private static int IsBearishHaramiCross(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            chart.IsTall(position, 0, 1.2M) &&
            second.IsDoji &&
            second.Body.High < first.Body.High &&
            second.Body.Low > first.Body.Low
            ? position + 1 : -1;
    }

    private static int IsTweezerTop(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.High == second.High &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1)
            ? position + 1 : -1;
    }

    private static int IsTweezerBottom(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.Low == second.Low &&
            chart.IsTall(position) &&
            chart.IsShort(position + 1)
            ? position + 1 : -1;
    }

    private static int IsBullishBelthold(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsBullishBelthold &&
            chart.IsTall(position)
            ? position : -1;
    }

    private static int IsBearishBelthold(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsBearishBelthold &&
            chart.IsTall(position)
            ? position : -1;
    }

    private static int IsUpsideGapTwoCrows(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            second.IsDark &&
            third.IsDark &&
            chart.IsTall(position) &&
            second.Body.Low > first.Body.High &&
            third.Body.High > second.Body.High &&
            third.Body.Low < first.Body.High
            ? position + 2 : -1;
    }

    private static int IsThreeBlackCrows(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsDark &&
            second.IsDark &&
            third.IsDark &&
            second.Body.High < first.Body.High &&
            second.Body.High > first.Body.Low &&
            third.Body.High < second.Body.High &&
            third.Body.High > second.Body.Low
            ? position + 2 : -1;
    }

    private static int IsThreeWhiteSoldiers(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];
        var third = chart.Candlesticks[position + 2];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsLight &&
            second.IsLight &&
            third.IsLight &&
            second.Body.Low > first.Body.Low &&
            second.Body.Low < first.Body.High &&
            third.Body.Low > second.Body.Low &&
            third.Body.Low < second.Body.High
            ? position + 2 : -1;
    }

    private static int IsBullishCounterattack(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bearish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bearish &&
            first.IsDark &&
            chart.IsTall(position) &&
            second.IsLight &&
            first.Close == second.Close
            ? position + 1 : -1;
    }

    private static int IsBearishCounterattack(Chart chart,
       int position,
       int numberRequired,
       int lengthOfPrologue,
       double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return chart.TrendValues[position].Sentiment == TrendSentiment.Bullish &&
            chart.PrologueSentiment(position) == TrendSentiment.Bullish &&
            first.IsLight &&
            chart.IsTall(position) &&
            second.IsDark &&
            first.Close == second.Close
            ? position + 1 : -1;
    }
}
