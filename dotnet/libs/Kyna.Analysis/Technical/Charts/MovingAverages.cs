using Kyna.Common;
using System.ComponentModel;

namespace Kyna.Analysis.Technical.Charts;

public enum MovingAverageType
{
    [Description("SMA")]
    Simple = 0,
    [Description("EMA")]
    Exponential = 1
}

public readonly struct MovingAverageKey : IEquatable<MovingAverageKey>
{
    public int Period { get; }
    public MovingAverageType Type { get; }
    public PricePoint PricePoint { get; }

    public MovingAverageKey(int period, PricePoint pricePoint = PricePoint.Close,
        MovingAverageType type = MovingAverageType.Simple)
    {
        if (period < 1)
            throw new ArgumentOutOfRangeException(nameof(period), $"{nameof(period)} cannot be less than 1.");
        Period = period;
        Type = type;
        PricePoint = pricePoint;
    }

    public override bool Equals(object? obj) => obj is MovingAverageKey key && Equals(key);

    public bool Equals(MovingAverageKey other) => Period == other.Period &&
               Type == other.Type &&
               PricePoint == other.PricePoint;

    public override int GetHashCode() => HashCode.Combine(Period, Type, PricePoint);

    public override readonly string ToString() =>
        $"{Type.GetEnumDescription()[0]}{Period}{PricePoint.GetEnumDescription()[0]}";

    public static bool operator ==(MovingAverageKey left, MovingAverageKey right) => left.Equals(right);

    public static bool operator !=(MovingAverageKey left, MovingAverageKey right) => !(left == right);
}

public readonly struct MovingAverage
{
    public MovingAverageKey Key { get; }
    public decimal[] Values { get; }

    public MovingAverage(MovingAverageKey key, decimal[] values)
    {
        Key = key;

        Values = (values?.Length ?? 0) == 0
            ? []
            : new decimal[values!.Length];

        if (Values.Length < key.Period)
            return;

        ComputeAverage compute = key.Type switch
        {
            MovingAverageType.Simple => ComputeSma,
            MovingAverageType.Exponential => ComputeEma,
            _ => throw new ArgumentException($"Unsupported moving average type: {key.Type}", nameof(key))
        };

        compute(key.Period, values!);
    }

    public MovingAverage(MovingAverageKey key, Ohlc[] prices)
        : this(key, prices?.Length > 0
            ? prices.Select(p => p.GetPricePoint(key.PricePoint)).ToArray()
            : throw new ArgumentException("Prices array cannot be null or empty.", nameof(prices)))
    { }

    private delegate void ComputeAverage(int period, decimal[] values);

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