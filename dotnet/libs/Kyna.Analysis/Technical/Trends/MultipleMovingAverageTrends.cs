using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Trends;

public class MultipleMovingAverageTrend : PriceTrendBase, ITrend
{
    private readonly List<MovingAverage> _movingAverages;
    private readonly int _lookbackPeriod;
    private readonly double _gamma;

    /// <summary>
    /// Represents a trend based on multiple moving averages.
    /// </summary>
    /// <param name="prices">Array of OHLC price data.</param>
    /// <param name="movingAverageKeys">Array of unique MovingAverageKeys, at least 2.</param>
    /// <param name="lookbackPeriod">Lookback period for standard deviation and volume (default: 20).</param>
    /// <param name="gamma">Sensitivity factor for score normalization (default: 1.0).</param>
    public MultipleMovingAverageTrend(Ohlc[] prices, MovingAverageKey[] movingAverageKeys,
        int lookbackPeriod = 20, double gamma = 1.0) : base(prices)
    {
        // Validate input: need at least 2 MAs
        if (movingAverageKeys.Length < 2)
        {
            throw new ArgumentException("At least two moving average keys are required.");
        }

        // Ensure unique keys and order by period (fastest to slowest)
        var uniqueKeys = movingAverageKeys.Distinct().OrderBy(k => k.Period).ToArray();
        if (uniqueKeys.Length < 2)
        {
            throw new ArgumentException("At least two unique moving average keys are required.");
        }

        // Initialize moving averages
        _movingAverages = new List<MovingAverage>();
        foreach (var key in uniqueKeys)
        {
            _movingAverages.Add(new MovingAverage(key,
                prices.Select(p => p.GetPricePoint(key.PricePoint)).ToArray()));
        }

        _lookbackPeriod = lookbackPeriod;
        _gamma = gamma;
    }

    /// <summary>
    /// Calculates the trend score for each time point, adjusted by volume.
    /// </summary>
    public void Calculate()
    {
        int numPairs = _movingAverages.Count - 1;

        for (int t = 0; t < _prices.Length; t++)
        {
            // Skip if not enough data for lookback
            if (t < _lookbackPeriod)
            {
                TrendValues[t] = 0.0;
                continue;
            }

            double totalScore = 0.0;

            // Process each consecutive pair of MAs
            for (int p = 0; p < numPairs; p++)
            {
                var maFast = _movingAverages[p].Values;
                var maSlow = _movingAverages[p + 1].Values;

                // Compute differences over lookback period
                var differences = new double[_lookbackPeriod];
                for (int i = 0; i < _lookbackPeriod; i++)
                {
                    int index = t - _lookbackPeriod + 1 + i;
                    differences[i] = (double)(maFast[index] - maSlow[index]);
                }

                // Calculate standard deviation of differences
                double stdDevDiff = CalculateStandardDeviation(differences);

                // Compute z-score for current day
                double diff_t = (double)(maFast[t] - maSlow[t]);
                double z_t = stdDevDiff == 0 ? 0.0 : diff_t / stdDevDiff;

                // Normalize to [-1, 1]
                double scorePair = (2.0 / Math.PI) * Math.Atan(_gamma * z_t);

                totalScore += scorePair;
            }

            // Base trend score: average of pairwise scores
            double baseTrendScore = totalScore / numPairs;

            // Calculate average volume over lookback period
            double avgVolume = CalculateAverageVolume(t);

            // Current volume
            double currentVolume = (double)_prices[t].Volume;

            // Volume factor: clamped between 0.5 and 2.0
            double volumeFactor = avgVolume > 0 ? currentVolume / avgVolume : 1.0;
            volumeFactor = Math.Max(0.5, Math.Min(2.0, volumeFactor));

            // Adjust trend score with volume and clamp to [-1, 1]
            double adjustedTrendScore = baseTrendScore * volumeFactor;
            TrendValues[t] = Math.Max(-1.0, Math.Min(1.0, adjustedTrendScore));
        }
    }

    /// <summary>
    /// Calculates the standard deviation of an array of values.
    /// </summary>
    private double CalculateStandardDeviation(double[] values)
    {
        if (values.Length <= 1)
            return 0.0;
        double mean = values.Average();
        double sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumOfSquares / values.Length);
    }

    /// <summary>
    /// Calculates the average volume over the lookback period ending at the specified index.
    /// </summary>
    private double CalculateAverageVolume(int endIndex)
    {
        if (endIndex < _lookbackPeriod - 1)
        {
            return (double)_prices.Take(endIndex + 1).Average(p => p.Volume);
        }
        return (double)_prices.Skip(endIndex - _lookbackPeriod + 1)
                             .Take(_lookbackPeriod)
                             .Average(p => p.Volume);
    }
}