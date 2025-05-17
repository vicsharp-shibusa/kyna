namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal record StatsBuild : DaoBase
{
    public StatsBuild() : base() { }

    public StatsBuild(Guid? processId = null) : base(processId) { }

    public Guid Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string ConfigContent { get; init; } = string.Empty;
}

internal record StatsDetail : DaoBase
{
    public StatsDetail() : base() { }

    public StatsDetail(Guid? processId = null) : base(processId) { }

    public Guid StatsBuildId { get; init; }
    public string Code { get; init; } = string.Empty;
    public DateOnly EntryDate { get; init; }
    public string StatType { get; init; } = string.Empty;
    public string StatKey { get; init; } = string.Empty;
    public double StatVal { get; init; }
    public string StatMeta { get; init; } = string.Empty;
}

internal record Stat : DaoBase
{
    public Stat() : base() { }

    public Stat(Guid? processId = null) : base(processId) { }

    public Guid StatsBuildId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string SubCategory { get; init; } = string.Empty;
    public string StatType { get; init; } = string.Empty;
    public string StatKey { get; init; } = string.Empty;
    public double StatVal { get; init; }
    public int SearchSize { get; init; }
    public int SampleSize { get; init; }
    public double? ConfidenceLower { get; init; }
    public double? ConfidenceUpper { get; init; }
}