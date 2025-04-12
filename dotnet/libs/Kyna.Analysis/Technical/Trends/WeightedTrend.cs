namespace Kyna.Analysis.Technical.Trends;

public record class WeightedTrend
{
    public ITrend Trend;
    public double Weight;

    public WeightedTrend(ITrend trend, double weight)
    {
        Trend = trend;
        Weight = weight;

        if ((trend?.TrendValues.Length ?? 0) == 0)
        {
            throw new ArgumentNullException(nameof(trend));
        }

        trend!.Calculate();
        TrendValues = new TrendValue[trend.TrendValues.Length];

        for (int t = 0; t < trend!.TrendValues.Length; t++)
        {
            var weightedValue = trend.TrendValues[t].Value * weight;
            var sentiment = weightedValue > 0
                ? TrendSentiment.Bullish
                : weightedValue < 0
                    ? TrendSentiment.Bearish
                    : TrendSentiment.Neutral;
            TrendValues[t] = new TrendValue(sentiment, weightedValue);
        }
    }

    public TrendValue[] TrendValues { get; }
    public string Name => $"{Trend.ToString}{Weight}";
}
