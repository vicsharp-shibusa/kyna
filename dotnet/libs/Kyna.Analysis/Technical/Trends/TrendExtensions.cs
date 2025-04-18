namespace Kyna.Analysis.Technical.Trends;

public static class TrendExtensions
{
    public static TrendSentiment AsSentiment(this double dbl)
    {
        return dbl switch
        {
            0D => TrendSentiment.Neutral,
            var x when x >= -1D && x < -0.75D => TrendSentiment.FullBear,
            var x when x < -0.25D => TrendSentiment.Bearish,
            var x when x < 0D => TrendSentiment.MildBear,
            var x when x < 0.25D => TrendSentiment.MildBull,
            var x when x < 0.75D => TrendSentiment.Bullish,
            var x when x <= 1.0D => TrendSentiment.FullBull,
            _ => TrendSentiment.Neutral
        };
    }

    public static bool IsBullish(this TrendSentiment sentiment)
    {
        return sentiment.HasFlag(TrendSentiment.FullBull) ||
            sentiment.HasFlag(TrendSentiment.Bullish) ||
            sentiment.HasFlag(TrendSentiment.MildBull);
    }

    public static bool IsBearish(this TrendSentiment sentiment)
    {
        return sentiment.HasFlag(TrendSentiment.FullBear) ||
            sentiment.HasFlag(TrendSentiment.Bearish) ||
            sentiment.HasFlag(TrendSentiment.MildBear);
    }

    public static bool IsBullishOrBearish(this TrendSentiment sentiment)
    {
        return IsBullish(sentiment) || IsBearish(sentiment);
    }

    public static bool IsSameSentiment(this TrendSentiment sentiment, TrendSentiment other)
    {
        return sentiment == other ||
            (sentiment.IsBullish() && other.IsBullish()) ||
            (sentiment.IsBearish() && other.IsBearish());
    }
}
