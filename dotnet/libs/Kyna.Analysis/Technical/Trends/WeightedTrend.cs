namespace Kyna.Analysis.Technical.Trends;

public record class WeightedTrend
{
    public ITrend Trend;
    public double Weight;

    public WeightedTrend(ITrend trend, double weight)
    {
        ArgumentNullException.ThrowIfNull(trend);
        if (weight <= 0 || weight > 1D)
            throw new ArgumentOutOfRangeException(nameof(weight));

        if ((trend?.TrendValues.Length ?? 0) == 0)
        {
            throw new ArgumentNullException(nameof(trend));
        }

        Trend = trend!;
        Weight = weight;

        Trend.Calculate();
        TrendValues = new double[Trend.TrendValues.Length];
            
        for (int t = 0; t < Trend!.TrendValues.Length; t++)
        {
            var weightedValue = Trend.TrendValues[t] * weight;
            TrendValues[t] = weightedValue;
        }
    }

    public double[] TrendValues { get; }
    public string Name => $"{Trend.Name}:{(Weight*100):F2}%";
}
