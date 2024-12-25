namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct General
{
    public string? Code;
    public string? Type;
    public string? Name;
    public string? Exchange;
    public string? CurrencyCode;
    public string? CurrencyName;
    public string? CurrencySymbol;
    public string? CountryName;
    public string? CountryIso;
    public string? Description;
    public string? Category;
    public DateOnly? UpdatedAt;
}