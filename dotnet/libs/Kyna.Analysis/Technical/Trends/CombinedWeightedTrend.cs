namespace Kyna.Analysis.Technical.Trends;

public class CombinedWeightedTrend : ITrend
{
    private readonly WeightedTrend[] _trends;
    private readonly int _length;
    private string? _name = null;

    public CombinedWeightedTrend(params WeightedTrend[] weightedTrends)
    {
        _length = weightedTrends![0].TrendValues.Length;
        int badTrendCount = weightedTrends.Count(t => t.TrendValues.Length != _length);
        if (badTrendCount > 0)
        {
            throw new ArgumentException($"All trends provided to the {nameof(CombinedWeightedTrend)} object must have the same length.");
        }
        var weightSum = weightedTrends.Select(t => t.Weight).Sum();
        if (weightSum != 1D)
        {
            throw new ArgumentException($"The total weight of trends provided to {nameof(CombinedWeightedTrend)} must equal 1.");
        }

        _trends = weightedTrends;
        TrendValues = new double[_length];
    }

    public string Name
    {
        get
        {
            _name ??= $"Combined: {string.Join(", ", _trends.Select(t => t.Name))}";
            return _name;
        }
    }

    public double[] TrendValues { get; }

    public void Calculate()
    {
        for (int i = 0; i < _trends[0].TrendValues.Length; i++)
        {
            TrendValues[i] = CalculateTrendForPosition(i);
        }
    }

    private double CalculateTrendForPosition(int position)
    {
        var value = 0D;
        if (position < 0 || position > (_length - 1))
        {
            return value;
        }

        foreach (var trend in _trends)
        {
            value += trend.TrendValues[position];
        }

        return value;
    }
}