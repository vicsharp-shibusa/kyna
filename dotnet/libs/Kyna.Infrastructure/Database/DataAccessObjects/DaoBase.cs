namespace Kyna.Infrastructure.Database.DataAccessObjects;

/// <summary>
/// Represents the base audit trail implementation.
/// This class contains <see cref="CreatedAt"/> and <see cref="ProcessId"/>.
/// </summary>
internal abstract record class AuditBase
{
    /// <summary>
    /// Instantiates a new instance with a new <see cref="ProcessId"/>.
    /// </summary>
    public AuditBase() : this(Guid.NewGuid())
    {
    }

    /// <summary>
    /// Instantiates a new instance with the provided <see cref="ProcessId"/>;
    /// a new <see cref="ProcessId"/> is created if the argument is null.
    /// </summary>
    /// <param name="processId"></param>
    public AuditBase(Guid? processId = null)
    {
        ProcessId = processId;
        _createdAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Gets the unique process id by which this record was created.
    /// </summary>
    public Guid? ProcessId { get; init; }

    private long _createdAtUnixMs;
    /// <summary>
    /// Gets the <see cref="CreatedAt"/> converted to Unix milliseconds.
    /// </summary>
    public long CreatedAtUnixMs { get => _createdAtUnixMs; init => _createdAtUnixMs = value; }

    /// <summary>
    /// Gets the DateTimeOffset for when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt
    {
        get => DateTimeOffset.FromUnixTimeMilliseconds(_createdAtUnixMs).ToLocalTime();
        init => _createdAtUnixMs = value.ToUnixTimeMilliseconds();
    }
}

/// <summary>
/// Represents the base DAO implementation.
/// </summary>
internal abstract record class DaoBase : AuditBase
{
    /// <summary>
    /// Instantiates a new instance with a new <see cref="ProcessId"/>.
    /// </summary>
    public DaoBase() : this((Guid?)null) { }

    /// <summary>
    /// Instantiates a new instance with the provided <see cref="ProcessId"/>;
    /// a new <see cref="ProcessId"/> is created if the argument is null.
    /// </summary>
    /// <param name="processId"></param>
    public DaoBase(Guid? processId = null) : base(processId)
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long _updatedAtUnixMs;

    /// <summary>
    /// Gets the <see cref="UpdatedAt"/> converted to Unix milliseconds.
    /// </summary>
    public long UpdatedAtUnixMs { get => _updatedAtUnixMs; init => _updatedAtUnixMs = value; }

    /// <summary>
    /// Gets the DateTimeOffset for when this record was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt
    {
        get => DateTimeOffset.FromUnixTimeMilliseconds(_updatedAtUnixMs).ToLocalTime();
        init => _updatedAtUnixMs = value.ToUnixTimeMilliseconds();
    }
}
