
namespace Kyna.Analysis.Technical.Charts;

public readonly struct ChartInfo : IEquatable<ChartInfo>
{
    public required string Code { get; init; }
    public string? Source { get; init; }
    public string? Industry { get; init; }
    public string? Sector { get; init; }
    public ChartInterval Interval { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is ChartInfo info && Equals(info);
    }

    public bool Equals(ChartInfo other)
    {
        return Code == other.Code &&
               Source == other.Source &&
               Industry == other.Industry &&
               Sector == other.Sector &&
               Interval == other.Interval;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code, Source, Industry, Sector, Interval);
    }
    public static bool operator ==(ChartInfo left, ChartInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ChartInfo left, ChartInfo right)
    {
        return !(left == right);
    }
}
