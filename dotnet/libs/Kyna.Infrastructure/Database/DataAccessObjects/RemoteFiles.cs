namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class RemoteFile : DaoBase
{
    public RemoteFile() : base() { }

    public RemoteFile(Guid? processId = null) : base(processId)
    {
    }

    public string? Source { get; init; }
    public string? Provider { get; init; }
    public string? Location { get; init; }
    public string? Name { get; init; }
    public DateOnly? UpdateDate { get; init; }
    public long? Size { get; init; }
    public string? HashCode { get; init; }
}