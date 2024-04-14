using System.Text.Json.Serialization;

namespace Kyna.Polygon.Models;

public struct Dividend
{
    [JsonPropertyName("cash_amount")]
    public decimal CashAmount;
    [JsonPropertyName("declaration_date")]
    public DateOnly DeclarationDate;
    [JsonPropertyName("dividend_type")]
    public string Type;
    [JsonPropertyName("ex_dividend_date")]
    public DateOnly ExDividendDate;
    public int? Frequency;
    [JsonPropertyName("pay_date")]
    public DateOnly PayDate;
    [JsonPropertyName("record_date")]
    public DateOnly RecordDate;
    public string Ticker;
}

public struct DividendResponse
{
    public Dividend[] Results;
    public string Status;
    [JsonPropertyName("request_id")]
    public string RequestId;
    [JsonPropertyName("next_url")]
    public string NextUrl;
}