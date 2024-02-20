using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct BalanceSheet
{
    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol;

    public IDictionary<string, BalanceSheetItem>? Quarterly;
    
    public IDictionary<string, BalanceSheetItem>? Yearly;
}
