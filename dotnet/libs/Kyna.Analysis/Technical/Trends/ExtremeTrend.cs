using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Trends;

public class ExtremeTrend : PriceTrendBase, ITrend
{
    private readonly int _lookbackPeriod;
    private readonly double _alpha;
    private readonly double _beta;

    /// <summary>
    /// Represents a trend based on highs and lows.
    /// </summary>
    /// <param name="prices">The prices (i.e., chart) for which to determine the trend.</param>
    /// <param name="lookbackPeriod">Defines how far back the algo looks when relating to highs and lows.</param>
    /// <param name="alpha">A number between 0 and 1 that decides how much the trend score focuses on the long-term
    /// trend vs. the short-term trend.</param>
    /// <param name="beta">A scaling factor that determines how sensitive the long-term trend (the slope) is to changes.</param>
    public ExtremeTrend(Ohlc[] prices, int lookbackPeriod = 20, double alpha = 0.5, double beta = 1.0)
        : base(prices)
    {
        if (alpha < 0 || alpha > 1D)
            throw new ArgumentOutOfRangeException(nameof(alpha));

        _lookbackPeriod = lookbackPeriod;  
        _alpha = alpha;
        _beta = beta;
    }

    public void Calculate()
    {
        var highs = new decimal[_prices.Length];
        var lows = new decimal[_prices.Length];

        // Calculate highs and lows (unchanged)
        for (int i = 0; i < _prices.Length; i++)
        {
            if (i < _lookbackPeriod - 1)
            {
                highs[i] = _prices.Take(i + 1).Max(p => p.High);
                lows[i] = _prices.Take(i + 1).Min(p => p.Low);
            }
            else
            {
                highs[i] = _prices.Skip(i - _lookbackPeriod + 1).Take(_lookbackPeriod).Max(p => p.High);
                lows[i] = _prices.Skip(i - _lookbackPeriod + 1).Take(_lookbackPeriod).Min(p => p.Low);
            }
        }

        // Calculate trend values with volume adjustment
        for (int i = 0; i < _prices.Length; i++)
        {
            if (i < _lookbackPeriod)
            {
                TrendValues[i] = 0D;
                continue;
            }

            // Existing trend calculation
            var slope = CalculateRegressionSlope(i, _lookbackPeriod);
            var normalizedSlope = (2D / Math.PI) * Math.Atan(_beta * slope);
            var recentHigh = highs[i];
            var recentLow = lows[i];
            var range = recentHigh - recentLow;
            var position = range == 0M ? 0M : (_prices[i].Close - recentLow) / range;
            var scaledPosition = 2M * position - 1M;
            var baseTrendScore = _alpha * normalizedSlope + (1 - _alpha) * (double)scaledPosition;

            // Volume adjustment
            var avgVolume = CalculateAverageVolume(i, _lookbackPeriod);
            var currentVolume = (double)_prices[i].Volume;
            var volumeFactor = avgVolume > 0 ? currentVolume / avgVolume : 1.0; // Avoid division by zero
            volumeFactor = Math.Max(0.5, Math.Min(2.0, volumeFactor)); // Clamp between 0.5 and 2.0

            // Apply volume-adjusted trend score
            TrendValues[i] = Math.Max(-1.0, Math.Min(1.0, baseTrendScore * volumeFactor));
        }
    }

    private double CalculateRegressionSlope(int endIndex, int lookbackPeriod)
    {
        decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = lookbackPeriod;

        for (int i = 0; i < n; i++)
        {
            int x = i;
            var y = _prices[endIndex - n + 1 + i].Close;
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return (double)slope;
    }

    private double CalculateAverageVolume(int endIndex, int lookbackPeriod)
    {
        if (endIndex < lookbackPeriod - 1)
        {
            // For early indices, use all available data up to endIndex
            return (double)_prices.Take(endIndex + 1).Average(p => p.Volume);
        }
        // For later indices, use the full lookback period
        return (double)_prices.Skip(endIndex - lookbackPeriod + 1)
                             .Take(lookbackPeriod)
                             .Average(p => p.Volume);
    }
}