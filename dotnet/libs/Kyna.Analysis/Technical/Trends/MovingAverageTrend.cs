using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Trends;

public class MovingAverageTrend : PriceTrendBase, ITrend
{
    private readonly MovingAverage _movingAverage;
    private readonly int _lookbackPeriod;
    private readonly double _alpha;
    private readonly double _beta;

    public override string Name => _movingAverage.Key.ToString();

    public MovingAverageTrend(MovingAverageKey movingAverageKey, Ohlc[] prices,
        int lookbackPeriod = 20, double alpha = 0.5, double beta = 1.0) : base(prices)
    {
        if (alpha < 0 || alpha > 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1.");

        _movingAverage = new(movingAverageKey,
            prices!.Select(p => p.GetPricePoint(movingAverageKey.PricePoint)).ToArray());
        _lookbackPeriod = lookbackPeriod;
        _alpha = alpha;
        _beta = beta;
    }

    public void Calculate()
    {
        // Convert moving average values to double for calculation precision
        var maValues = _movingAverage.Values.Select(v => (double)v).ToArray();

        // Precompute recent highs and lows over the lookback period
        var maHighs = new double[maValues.Length];
        var maLows = new double[maValues.Length];

        for (int i = 0; i < maValues.Length; i++)
        {
            if (i < _lookbackPeriod - 1)
            {
                maHighs[i] = maValues.Take(i + 1).Max();
                maLows[i] = maValues.Take(i + 1).Min();
            }
            else
            {
                maHighs[i] = maValues.Skip(i - _lookbackPeriod + 1).Take(_lookbackPeriod).Max();
                maLows[i] = maValues.Skip(i - _lookbackPeriod + 1).Take(_lookbackPeriod).Min();
            }
        }

        // Calculate trend values with volume adjustment
        for (int i = 0; i < _prices.Length; i++)
        {
            if (i < _lookbackPeriod)
            {
                TrendValues[i] = 0.0; // Insufficient data for trend
                continue;
            }

            // Slope of the moving average over lookback period
            var slope = CalculateRegressionSlope(maValues, i - _lookbackPeriod + 1, _lookbackPeriod);
            var normalizedSlope = (2.0 / Math.PI) * Math.Atan(_beta * slope);

            // Position of current moving average within recent range
            var recentHigh = maHighs[i];
            var recentLow = maLows[i];
            var range = recentHigh - recentLow;
            var position = range == 0 ? 0.0 : (maValues[i] - recentLow) / range;
            var scaledPosition = 2.0 * position - 1.0;

            // Combine slope and position for base trend score
            var baseTrendScore = _alpha * normalizedSlope + (1 - _alpha) * scaledPosition;

            // Calculate average volume over the lookback period
            var avgVolume = CalculateAverageVolume(i);

            // Current volume
            var currentVolume = (double)_prices[i].Volume;

            // Volume factor (ratio of current to average volume)
            var volumeFactor = avgVolume > 0 ? currentVolume / avgVolume : 1.0;

            // Clamp volume factor between 0.5 and 2.0
            volumeFactor = Math.Max(0.5, Math.Min(2.0, volumeFactor));

            // Adjust trend score with volume factor and clamp to [-1, 1]
            var adjustedTrendScore = baseTrendScore * volumeFactor;
            TrendValues[i] = Math.Max(-1.0, Math.Min(1.0, adjustedTrendScore));
        }
    }

    private double CalculateRegressionSlope(double[] values, int startIndex, int length)
    {
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < length; i++)
        {
            int x = i; // Time index
            double y = values[startIndex + i]; // Moving average value
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }
        double slope = (length * sumXY - sumX * sumY) / (length * sumX2 - sumX * sumX);
        return slope;
    }

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