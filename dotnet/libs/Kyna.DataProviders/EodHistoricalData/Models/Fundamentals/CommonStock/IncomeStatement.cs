using System.Text.Json.Serialization;

namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct IncomeStatement
{
    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol;
    public IDictionary<string, IncomeStatementItem>? Quarterly;
    public IDictionary<string, IncomeStatementItem>? Yearly;
}
