namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal record class ApiTransaction
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
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
