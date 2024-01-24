using Kyna.Common.Logging;

namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Log : LogBase
{
    public string? LogLevel { get; init; }
    public string? Message { get; init; }
    public string? Exception { get; init; }
    public string? Scope { get; init; }
}

internal sealed record class AppEvent : LogBase
{
    public int EventId { get; init; }
    public string? EventName { get; init; }
}

internal abstract record class LogBase
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public Guid? ProcessId { get; init; }
}