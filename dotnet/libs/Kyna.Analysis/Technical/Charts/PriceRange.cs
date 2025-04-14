using Kyna.Common;

namespace Kyna.Analysis.Technical.Charts;

public record class PriceRange
{
    public PriceRange(decimal high, decimal low)
    {
        if (low > high)
            (low, high) = (high, low);

        High = Math.Round(high, Constants.MoneyPrecision);
        Low = Math.Round(low, Constants.MoneyPrecision);
    }

    public decimal High { get; }
    public decimal Low { get; }
    public decimal Length => High - Low;
    public decimal MidPoint => (High + Low) / 2M;

    public bool Overlaps(PriceRange other) => Low <= other.High && High >= other.Low;

    public bool Contains(decimal price) => price >= Low && price <= High;

    public static PriceRange Average(IEnumerable<PriceRange> ranges)
    {
        var avgHigh = ranges.Average(r => r.High);
        var avgLow = ranges.Average(r => r.Low);
        return new PriceRange(avgHigh, avgLow);
    }
}
