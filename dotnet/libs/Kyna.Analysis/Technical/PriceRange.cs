namespace Kyna.Analysis.Technical;

public record class PriceRange
{
    // Number of places to the right of the decimal point in money calculations.
    protected const int MoneyPrecision = 4;

    public PriceRange(decimal high, decimal low)
    {
        if (low > high)
        {
            throw new ArgumentException($"{nameof(low)} must be lower than or equal to {nameof(high)}");
        }

        High = Math.Round(high, MoneyPrecision);
        Low = Math.Round(low, MoneyPrecision);
    }

    public decimal High { get; }
    public decimal Low { get; }
    public decimal Length => High - Low;
    public decimal MidPoint => (High + Low) / 2M;
}
