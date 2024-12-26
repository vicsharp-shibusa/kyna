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
        : this(key, prices.Select(p => p.GetPricePoint(key.PricePoint)).ToArray())
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

        if (key.Period < 2 || values.Length < key.Period)
        {
            Values = Enumerable.Repeat(0M, values.Length).ToArray();
            return;
        }

        Values = new decimal[values.Length];

        for (int i = 0; i < key.Period - 1; i++)
        {
            Values[i] = 0M;
        }

        if (key.Type == MovingAverageType.Simple)
        {
            decimal sum = values.Take(key.Period).Sum();
            Values[key.Period - 1] = sum / key.Period;

            for (int i = key.Period; i < values.Length; i++)
            {
                sum += values[i] - values[i - key.Period];
                Values[i] = sum / key.Period;
            }
        }

        if (key.Type == MovingAverageType.Exponential)
        {
            decimal factor = 2M / (key.Period + 1);

            Values[key.Period - 1] = values.Take(key.Period).Average();

            for (int i = key.Period; i < values.Length; i++)
            {
                // https://sciencing.com/calculate-exponential-moving-averages-8221813.html
                Values[i] = (values[i] - Values[i - 1]) * factor + Values[i - 1];
            }
        }
    }
}