namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class ApiTransaction : AuditBase
{
    public ApiTransaction() : base() { }
    public ApiTransaction(Guid? processId = null) : base(processId) { }

    public string? Source { get; init; }
    public string? Category { get; init; }
    public string? SubCategory { get; init; }
    public string? RequestUri { get; init; }
    public string? RequestMethod { get; init; }
    public string? RequestPayload { get; init; }
    public string? RequestHeaders { get; init; }
    public string? ResponseHeaders { get; init; }
    public string? ResponseStatusCode { get; init; }
    public string? ResponseBody { get; init; }
}

internal sealed record class ApiTransactionForMigration
{
    public int Id { get; init; }
    public required string Source { get; init; }
    public required string Category { get; init; }
    public required string SubCategory { get; init; }
    public required string ResponseStatusCode { get; init; }
    public Guid? ProcessId { get; init; }
    public bool DoMigrate { get; internal set; } = false;
    public bool DeleteFromSource { get; internal set; } = false;
}