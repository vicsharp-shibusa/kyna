using Kyna.Common;

namespace Kyna.Analysis.Technical.Charts;

public record class PriceRange
{
    public PriceRange(decimal high, decimal low)
    {
        if (low > high)
        {
            throw new ArgumentException($"{nameof(low)} must be lower than or equal to {nameof(high)}");
        }

        High = Math.Round(high, Constants.MoneyPrecision);
        Low = Math.Round(low, Constants.MoneyPrecision);
    }

    public decimal High { get; }
    public decimal Low { get; }
    public decimal Length => High - Low;
    public decimal MidPoint => (High + Low) / 2M;
}
