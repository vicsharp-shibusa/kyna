namespace Kyna.EodHistoricalData.Models;

public struct Dividend
{
    public DateOnly Date;
    public DateOnly DeclarationDate;
    public DateOnly RecordDate;
    public DateOnly PaymentDate;
    public string Period;
    public decimal Value;
    public decimal UnadjustedValue;
    public string Currency;
}

