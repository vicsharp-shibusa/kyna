namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal abstract record class AuditBase
{
    public AuditBase() : this(Guid.NewGuid())
    {
    }

    public AuditBase(Guid? processId = null)
    {
        ProcessId = processId;
        _createdAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public Guid? ProcessId { get; init; }

    private long _createdAtUnixMs;
    public long CreatedAtUnixMs { get => _createdAtUnixMs; init => _createdAtUnixMs = value; }

    public DateTimeOffset CreatedAt
    {
        get => DateTimeOffset.FromUnixTimeMilliseconds(_createdAtUnixMs).ToLocalTime();
        init => _createdAtUnixMs = value.ToUnixTimeMilliseconds();
    }
}

internal abstract record class DaoBase : AuditBase
{
    public DaoBase() : this((Guid?)null) { }

    public DaoBase(Guid? processId = null) : base(processId)
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long _updatedAtUnixMs;
    public long UpdatedAtUnixMs { get => _updatedAtUnixMs; init => _updatedAtUnixMs = value; }

    public DateTimeOffset UpdatedAt
    {
        get => DateTimeOffset.FromUnixTimeMilliseconds(_updatedAtUnixMs).ToLocalTime();
        init => _updatedAtUnixMs = value.ToUnixTimeMilliseconds();
    }
}
