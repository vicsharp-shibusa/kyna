using System.ComponentModel;

namespace Kyna.Analysis;

public enum EntityType
{
    Unknown = 0,
    [Description("Common Stock")]
    CommonStock,
    [Description("ETF")]
    ExchangeTradedFund
}

public class InvestableEntity(string source, string code)
{
    public string Source { get; } = source;
    public string Code { get; } = code;
    public EntityType Type { get; init; }
    public string? Name { get; init; }
    public string? Exchange { get; init; }
    public string? Country { get; init; }
    public string? Currency { get; init; }
    public string? Sector { get; init; }
    public string? Industry { get; init; }
    public string? GicSector { get; init; }
    public string? GicGroup { get; init; }
    public string? GicIndustry { get; init; }
    public string? GicSubIndustry { get; init; }
    public string? WebUrl { get; init; }
    public string? Phone { get; init; }
    public Split[] Splits { get; init; } = [];
    public Dividend[] Dividends { get; init; } = [];
    public bool HasSplits => Splits.Length > 0;
    public bool HasDividends => Dividends.Length > 0;
    public bool IsDelisted { get; init; }
    public bool IsIgnored { get; init; }
    public string? IgnoredReason { get; init; }
}
