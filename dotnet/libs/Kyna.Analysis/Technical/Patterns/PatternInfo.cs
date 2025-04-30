using Kyna.Common;

namespace Kyna.Analysis.Technical.Patterns;

public readonly struct PatternInfo : IEquatable<PatternInfo>
{
    public PatternName PatternName { get; init; }

    public string Name => PatternName.GetEnumDescription();

    private readonly int _numberRequired;

    /// <summary>
    /// Gets the number of candles or OHLCs required to complete the pattern.
    /// Guaranteed to always be greater than zero (0).
    /// </summary>
    public int NumberRequired
    {
        get => _numberRequired;
        init
        {
            _numberRequired = value < 1
                ? throw new ArgumentOutOfRangeException(nameof(NumberRequired),
                    $"{nameof(PatternInfo)}.{nameof(NumberRequired)} must be greater than zero.")
                : value;
        }
    }

    public TrendSentiment RequiredSentiment { get; init; }
    public TrendSentiment ExpectedSentiment { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is PatternInfo info && Equals(info);
    }

    public bool Equals(PatternInfo other)
    {
        return PatternName == other.PatternName &&
               NumberRequired == other.NumberRequired &&
               RequiredSentiment == other.RequiredSentiment &&
               ExpectedSentiment == other.ExpectedSentiment;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PatternName, NumberRequired, RequiredSentiment, ExpectedSentiment);
    }

    public static bool operator ==(PatternInfo left, PatternInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PatternInfo left, PatternInfo right)
    {
        return !(left == right);
    }
}
