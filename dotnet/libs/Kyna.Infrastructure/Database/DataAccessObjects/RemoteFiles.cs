namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class RemoteFile : DaoBase
{
    public RemoteFile() : base() { }

    public RemoteFile(Guid? processId = null) : base(processId)
    {
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Source { get; init; }
    public string? Provider { get; init; }
    public string? Location { get; init; }
    public string? SourceName { get; init; }
    public string? LocalName { get; init; }
    public DateOnly? UpdateDate { get; init; }
    public long? Size { get; init; }
    public string? HashCode { get; init; }

    private long? _migratedAtUnixMs;
    public long? MigratedAtUnixMs { get => _migratedAtUnixMs; init => _migratedAtUnixMs = value; }

    public DateTimeOffset? MigratedAt
    {
        get => _migratedAtUnixMs.GetValueOrDefault() > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(_migratedAtUnixMs!.Value).ToLocalTime()
            : null;
        internal set => _migratedAtUnixMs = value.HasValue ? value.Value.ToUnixTimeMilliseconds() : null;
    }

    internal bool IsNameMatch(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return (LocalName?.IsMatch(name) ?? false) || (SourceName?.IsMatch(name) ?? false);
    }
}

file static class StringExtensions
{
    public static bool IsMatch(this string str, string candidate)
    {
        ArgumentNullException.ThrowIfNull(str);
        if (string.IsNullOrWhiteSpace(candidate))
            return false;
        return str.StartsWith(candidate, StringComparison.OrdinalIgnoreCase);
    }
}