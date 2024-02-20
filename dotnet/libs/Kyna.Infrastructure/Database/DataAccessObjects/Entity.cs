namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Entity : DaoEntityBase
{
    public Entity()
    {
        Source = "";
        Code = "";
        Type = "";
        Name = "";
        Exchange = "";
        Currency = "";
        Country = "";
    }

    public Entity(string source, string code, string type, string name, string exchange, string currency, string country)
    {
        Source = source;
        Code = code;
        Type = type;
        Name = name;
        Exchange = exchange;
        Currency = currency;
        Country = country;
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public string Type { get; init; }
    public string Name { get; init; }
    public string Exchange { get; init; }
    public string Country { get; init; }
    public string Currency { get; init; }
    public bool Delisted { get; init; }
    public bool Ignored { get; init; }
    public bool HasSplits { get; init; }
    public bool HasDividends { get; init; }
    public bool HasPriceActions { get; init; }
    public bool HasFundamentals { get; init; }
    public DateOnly? LastPriceActionDate { get; init; }
    public DateOnly? LastFundamentalDate { get; init; }
    public DateOnly? NextFundamentalDate { get; init; }
    public string? IgnoredReason { get; init; }
    public string? Sector { get; init; }
    public string? Industry { get; init; }
    public string? GicSector { get; init; }
    public string? GicGroup { get; init; }
    public string? GicIndustry { get; init; }
    public string? GicSubIndustry { get; init; }
    public string? WebUrl { get; init; }
    public string? Phone { get; init; }
}