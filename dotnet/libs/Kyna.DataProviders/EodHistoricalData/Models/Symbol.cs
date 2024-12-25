namespace Kyna.DataProviders.EodHistoricalData.Models;

public struct Symbol
{
    public string? Code;
    public string? Name;
    public string? Country;
    public string? Exchange;
    public string? Currency;
    public string? Type;

    public override readonly string ToString()
    {
        string val = $"{Code} {Exchange} {Type}";
        while (val.Contains("  "))
        {
            val = val.Replace("  ", " ").Trim();
        }
        return string.IsNullOrWhiteSpace(val) ? "" : val;
    }
}
