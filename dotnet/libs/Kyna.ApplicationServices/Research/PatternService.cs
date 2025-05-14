using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Charts;

namespace Kyna.ApplicationServices.Research;

public static class PatternService
{
    public static IEnumerable<ResearchResult> FindRandom(Chart chart, int lengthOfEpilogue = 15,
        int increment = 5, PricePoint pricePoint = PricePoint.Close)
    {
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentOutOfRangeException.ThrowIfLessThan(lengthOfEpilogue, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lengthOfEpilogue, chart.Length);

        int i = 0;
        while (i < chart.Length)
        {
            var pos = Random.Shared.Next(i, i + 1 + increment);
            if (pos + lengthOfEpilogue + 2 >= chart.Length)
                break;

            var start = pos + 1;
            var finish = start + lengthOfEpilogue + 1;

            yield return new ResearchResult(chart.Info, chart.PriceActions[i].Date,
                StatType.Signal, "Random", new StatMeta(chart.TrendValues[i], lengthOfEpilogue),
                GetTotalPriceDeviationOverSpan(chart.GetSpan(start, finish),
                chart.PriceActions[pos].GetPricePoint(pricePoint)));

            i += increment;
        }
    }

    private static decimal GetTotalPriceDeviationOverSpan(ChartSpan chartSpan, decimal price,
        PricePoint pricePoint = PricePoint.Close) =>
        chartSpan.PriceActions.Select(k => k.GetPricePoint(pricePoint) - price).Sum();

    public static (double Lower, double Upper) CalculateWilsonScoreInterval(double proportion, int sampleSize, double confidenceLevel = 0.95)
    {
        if (sampleSize <= 0 || proportion < 0 || proportion > 1)
            return (0.0, 0.0);

        // Z-score for the confidence level (e.g., 1.96 for 95%)
        double z = confidenceLevel switch
        {
            0.90 => 1.645,
            0.95 => 1.96,
            0.99 => 2.576,
            _ => 1.96 // Default to 95%
        };

        double zSquared = z * z;
        double zSquaredOverN = zSquared / sampleSize;
        double zSquaredOver2N = zSquared / (2 * sampleSize);
        double zSquaredOver4NSquared = zSquared / (4 * sampleSize * sampleSize);
        double pComplement = 1 - proportion;
        double varianceTerm = (proportion * pComplement / sampleSize) + zSquaredOver4NSquared;
        double zTimesSqrt = z * Math.Sqrt(varianceTerm);

        double denominator = 1 + zSquaredOverN;
        double lower = (proportion + zSquaredOver2N - zTimesSqrt) / denominator;
        double upper = (proportion + zSquaredOver2N + zTimesSqrt) / denominator;

        // Ensure bounds are within [0, 1]
        lower = Math.Max(0.0, Math.Min(1.0, lower));
        upper = Math.Max(0.0, Math.Min(1.0, upper));

        return (lower, upper);
    }
}
