using System.Text.Json.Serialization;

namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct Financials
{
    [JsonPropertyName("balance_sheet")]
    public BalanceSheet BalanceSheet;

    [JsonPropertyName("cash_flow")]
    public CashFlow CashFlow;

    [JsonPropertyName("income_statement")]
    public IncomeStatement IncomeStatement;
}
