namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Log : AuditBase
{
    public string? LogLevel { get; init; }
    public string? Message { get; init; }
    public string? Exception { get; init; }
    public string? Scope { get; init; }
}

internal sealed record class AppEvent : AuditBase
{
    public int EventId { get; init; }
    public string? EventName { get; init; }
}
