namespace Kyna.Backtests.Accounting;

internal struct Position : IEquatable<Position>
{
    public required DateOnly Date { get; init; }
    public required string Instrument { get; init; }
    public required int Quantity { get; internal set; }
    public decimal? InstrumentMostRecentClose { get; internal set; }
    public required decimal EntryPrice { get; init; }
    public readonly decimal BookValue => EntryPrice * Quantity;
    public readonly decimal LiquidValue =>
        InstrumentMostRecentClose.HasValue
        ? InstrumentMostRecentClose.Value * Quantity : BookValue;
    public Position CopyWithNewDate(DateOnly date)
    {
        return new Position()
        {
            Date = date,
            Instrument = Instrument,
            Quantity = Quantity,
            InstrumentMostRecentClose = InstrumentMostRecentClose,
            EntryPrice = EntryPrice
        };
    }
    public override readonly bool Equals(object? obj)
    {
        return obj is Position position && Equals(position);
    }

    public readonly bool Equals(Position other)
    {
        return Date.Equals(other.Date) &&
               Instrument == other.Instrument;
    }

    public override readonly int GetHashCode() => HashCode.Combine(Date, Instrument);

    public override readonly string? ToString() => $"{Instrument} {Date:yyyy-MM-dd}";

    public static bool operator ==(Position left, Position right) => left.Equals(right);

    public static bool operator !=(Position left, Position right) => !(left.Equals(right));
}
