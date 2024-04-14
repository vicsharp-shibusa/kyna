namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class RemoteFile
{
    public string? Source { get; init; }
    public string? Provider { get; init; }
    public string? Location { get; init; }
    public string? Name { get; init; }
    public DateOnly? UpdateDate { get; init; }
    public long? Size { get; init; }
    public string? HashCode { get; init; }
    public Guid? ProcessId { get; init; }
    public long CreatedTicksUtc { get; init; } = DateTime.UtcNow.Ticks;
    public long UpdatedTicksUtc { get; init; } = DateTime.UtcNow.Ticks;
}