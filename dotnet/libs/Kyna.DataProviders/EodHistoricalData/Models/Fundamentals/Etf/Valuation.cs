using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.Etf;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Valuation
{
    [JsonPropertyName("price/prospective earnings")]
    public string? PriceProspectiveEarnings;
    [JsonPropertyName("price/book")]
    public string? PriceBook;
    [JsonPropertyName("price/sales")]
    public string? PriceSales;
    [JsonPropertyName("price/cash flow")]
    public string? PriceCashFlow;
    [JsonPropertyName("dividend-yield factor")]
    public string? DividendYieldFactor;
}
