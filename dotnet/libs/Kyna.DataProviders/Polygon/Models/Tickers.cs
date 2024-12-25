using System.Text.Json.Serialization;

namespace Kyna.DataProviders.Polygon.Models;

public struct TickerResponse
{
    public Ticker[] Results;
    public string Status;
    [JsonPropertyName("request_id")]
    public string RequestId;
    public int Count;
    [JsonPropertyName("next_url")]
    public string NextUrl;
}

public struct TickerDetailResponse
{
    public TickerDetail Results;
    public string Status;
    [JsonPropertyName("request_id")]
    public string RequestId;
    public int Count;
    [JsonPropertyName("next_url")]
    public string NextUrl;
}

public struct Ticker
{
    [JsonPropertyName("ticker")]
    public string Code;
    public string Name;
    public string Market;
    public string Locale;
    [JsonPropertyName("primary_exchange")]
    public string PrimaryExchange;
    public string Type;
    public bool Active;
    [JsonPropertyName("currency_name")]
    public string CurrencyName;
    [JsonPropertyName("composite_figi")]
    public string CompositeFigi;
    [JsonPropertyName("share_class_figi")]
    public string ShareClassFigi;
    [JsonPropertyName("last_updated_figi")]
    public DateTime LastUpdatedUtc;
}

public struct Address
{
    public string Address1;
    public string? Address2;
    public string City;
    public string State;
    [JsonPropertyName("postal_code")]
    public string PostalCode;
}

public struct Branding
{
    [JsonPropertyName("logo_url")]
    public string LogoUrl;
    [JsonPropertyName("icon_url")]
    public string IconUrl;
}

public struct TickerDetail
{
    public string Ticker;
    public string Name;
    public string Market;
    public string Locale;
    [JsonPropertyName("primary_exchange")]
    public string PrimaryExchange;
    public string Type;
    public bool Active;
    [JsonPropertyName("currency_name")]
    public string CurrencyName;
    public string Cik;
    [JsonPropertyName("composite_figi")]
    public string CompositeFigi;
    [JsonPropertyName("share_class_figi")]
    public string ShareClassFigi;
    [JsonPropertyName("market_cap")]
    public double MarketCap;
    [JsonPropertyName("phone_number")]
    public string PhoneNumber;
    public Address Address;
    public string Description;
    [JsonPropertyName("sic_code")]
    public string SicCode;
    [JsonPropertyName("sic_description")]
    public string SicDescription;
    [JsonPropertyName("ticker_root")]
    public string TickerRoot;
    [JsonPropertyName("homepage_url")]
    public string HomepageUrl;
    [JsonPropertyName("total_employees")]
    public int TotalEmployees;
    [JsonPropertyName("list_date")]
    public DateOnly ListDate;
    public Branding Branding;
    [JsonPropertyName("share_class_shares_outstanding")]
    public long SharesOutstanding;
    [JsonPropertyName("round_lot")]
    public int RoundLot;
}