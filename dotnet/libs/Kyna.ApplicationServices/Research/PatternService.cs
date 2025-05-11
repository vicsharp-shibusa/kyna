using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Charts;

namespace Kyna.ApplicationServices.Research;

public sealed class PatternService
{
    public PatternService() { }

    public IEnumerable<ResearchResult> FindRandom(Chart chart, int lengthOfEpilogue = 15,
        int increment = 5, PricePoint pricePoint = PricePoint.Close)
    {
        ArgumentNullException.ThrowIfNull(chart);
        if (lengthOfEpilogue < 1)
            throw new ArgumentOutOfRangeException($"{nameof(lengthOfEpilogue)} must be a positive number.");
        if (lengthOfEpilogue > chart.Length)
            throw new ArgumentOutOfRangeException($"{nameof(lengthOfEpilogue)} cannot be greater than length of chart.");

        int i = 0;
        while (i < chart.Length)
        {
            var pos = Random.Shared.Next(i, i + 1 + increment);
            if (pos + lengthOfEpilogue + 2 >= chart.Length)
                break;

            var start = pos + 1;
            var finish = start + lengthOfEpilogue + 1;
            if (finish > chart.Length - 1)
                Console.WriteLine("here is the problem");
            var val = GetTotalPriceDeviationOverSpan(chart.GetSpan(start, finish), chart.PriceActions[pos].GetPricePoint(pricePoint));

            yield return new ResearchResult(chart.Info, chart.PriceActions[i].Date,
                StatType.Signal, "Random", new StatMeta(chart.TrendValues[i], lengthOfEpilogue));

            i += increment;
        }
    }

    private static decimal GetTotalPriceDeviationOverSpan(ChartSpan chartSpan, decimal price,
        PricePoint pricePoint = PricePoint.Close) =>
        chartSpan.PriceActions.Select(k => k.GetPricePoint(pricePoint) - price).Sum();
}
