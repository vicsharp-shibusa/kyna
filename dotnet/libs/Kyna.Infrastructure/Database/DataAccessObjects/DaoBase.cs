namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal abstract record class DaoBase
{
    // Number of places to the right of the decimal point in money calculations.
    protected const int MoneyPrecision = 4;

    public DaoBase(Guid? processId = null)
    {
        ProcessId = processId;
    }

    public long CreatedTicksUtc { get; init; } = DateTime.UtcNow.Ticks;
    public DateTime CreatedUtc => new(CreatedTicksUtc, DateTimeKind.Utc);
    public long UpdatedTicksUtc { get; init; } = DateTime.UtcNow.Ticks;
    public DateTime UpdatedUtc => new(UpdatedTicksUtc, DateTimeKind.Utc);
    public Guid? ProcessId { get; init; }
}
