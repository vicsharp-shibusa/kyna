namespace Kyna.Analysis.Technical.Charts;

public enum MovingAverageType
{
    Simple = 0,
    Exponential = 1
}

public struct MovingAverageKey(int period, PricePoint pricePoint = PricePoint.Close,
    MovingAverageType type = MovingAverageType.Simple)
{
    public int Period = period;
    public MovingAverageType Type = type;
    public PricePoint PricePoint = pricePoint;

    public override readonly string ToString() =>
        $"{Type.ToString()[0]}{Period}{PricePoint.ToString()[0]}";
}

public struct MovingAverage
{
    public MovingAverageKey Key;
    public decimal[] Values;

    public MovingAverage(MovingAverageKey key, Ohlc[] prices)
        : this(key, prices?.Length > 0
            ? prices.Select(p => p.GetPricePoint(key.PricePoint)).ToArray()
            : throw new ArgumentException("Prices array cannot be null or empty.", nameof(prices)))
    {
    }

    public MovingAverage(MovingAverageKey key, decimal[] values)
    {
        Key = key;

        if (values == null)
        {
            Values = [];
            return;
        }

        if (values.Length == 0)
        {
            Values = [];
            return;
        }

        Values = new decimal[values.Length];

        if (values.Length < key.Period)
        {
            Array.Fill(Values, 0M);
            return;
        }

        // Pad initial values
        for (int i = 0; i < Math.Min(key.Period - 1, values.Length); i++)
        {
            Values[i] = 0M;
        }

        switch (key.Type)
        {
            case MovingAverageType.Simple:
                ComputeSma(key.Period, values);
                break;

            case MovingAverageType.Exponential:
                ComputeEma(key.Period, values);
                break;

            default:
                throw new ArgumentException($"Unsupported moving average type: {key.Type}", nameof(key));
        }
    }

    private void ComputeSma(int period, decimal[] values)
    {
        decimal sum = 0M;
        for (int i = 0; i < period; i++)
        {
            sum += values[i];
        }
        Values[period - 1] = sum / period;

        for (int i = period; i < values.Length; i++)
        {
            sum += values[i] - values[i - period];
            Values[i] = sum / period;
        }
    }

    private void ComputeEma(int period, decimal[] values)
    {
        decimal factor = 2M / (period + 1);
        decimal sum = 0M;
        for (int i = 0; i < period; i++)
        {
            sum += values[i];
        }
        Values[period - 1] = sum / period; // Initial SMA

        for (int i = period; i < values.Length; i++)
        {
            Values[i] = (values[i] - Values[i - 1]) * factor + Values[i - 1];
        }
    }
}