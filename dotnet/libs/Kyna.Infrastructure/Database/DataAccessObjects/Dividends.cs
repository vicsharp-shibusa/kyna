namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Dividend : DaoBase
{
    public Dividend(string source, string code, string type, Guid? processId = null) : base(processId)
    {
        Source = source;
        Code = code;
        Type = type;
    }

    public Dividend(string source, string code, string type,
        DateOnly? declarationDate,
        DateOnly? exDividendDate,
        DateOnly? payDate,
        DateOnly? recordDate,
        int? frequency,
        decimal amount,
        long createdTicksUtc,
        long updatedTicksUtc,
        Guid? processId = null) : base(processId)
    {
        Source = source;
        Code = code;
        Type = type;
        DeclarationDate = declarationDate;
        ExDividendDate = exDividendDate;
        PayDate = payDate;
        RecordDate = recordDate;
        Frequency = frequency;
        Amount = amount;
        CreatedTicksUtc = createdTicksUtc;
        UpdatedTicksUtc = updatedTicksUtc;
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public string Type { get; init; }
    public DateOnly? DeclarationDate { get; init; }
    public DateOnly? ExDividendDate { get; init; }
    public DateOnly? PayDate { get; init; }
    public DateOnly? RecordDate { get; init; }
    public int? Frequency { get; init; }
    public decimal Amount { get; init; }
}
