using System.Text;

namespace Kyna.Analysis.Technical.Trends;

public class CombinedTrend : TrendBase, ITrend
{
    private readonly WeightedTrend[] _trends;
    private readonly int _length;
    private string? _name = null;

    public CombinedTrend(params WeightedTrend[] weightedTrends)
        : base((weightedTrends?.Length ?? 0) == 0
            ? 0 : (weightedTrends?[0].TrendValues.Length ?? 0))
    {
        _length = weightedTrends![0].TrendValues.Length;
        int badTrendCount = weightedTrends.Count(t => t.TrendValues.Length != _length);
        if (badTrendCount > 0)
        {
            throw new ArgumentException($"All trends provided to the {nameof(CombinedTrend)} object must have the same length.");
        }
        var weightSum = weightedTrends.Select(t => t.Weight).Sum();
        if (weightSum != 1D)
        {
            throw new ArgumentException($"The total weight of trends provided to {nameof(CombinedTrend)} must equal 1.");
        }

        _trends = weightedTrends;
    }

    public string Name
    {
        get
        {
            if (_name == null )
            {
                StringBuilder sb = new();
                foreach (var t in _trends)
                {
                    sb.Append(t.Name);
                }
                _name = sb.ToString();
            }
            return _name;
        }
    }

    public void Calculate()
    {
        for (int i = 0; i < _trends[0].TrendValues.Length; i++)
        {
            var (Sentiment, Value) = CalculateTrendForPosition(i);
            TrendValues[i] = new TrendValue(Sentiment, Value);
        }
    }

    private (TrendSentiment Sentiment, double Value) CalculateTrendForPosition(int position)
    {
        if (position < 0 || position > (_length - 1))
        {
            return (TrendSentiment.None, 0D);
        }

        var value = 0D;
        foreach (var trend in _trends)
        {
            value += trend.TrendValues[position].Value;
        }
        var sentiment = value > 0
            ? TrendSentiment.Bullish
            : value < 0
                ? TrendSentiment.Bearish
                : TrendSentiment.Neutral;

        return (sentiment, value);
    }
}