namespace Kyna.Infrastructure.Database.DataAccessObjects.Reports;

internal sealed record SignalCounts
{
    public SignalCounts(string signalName,
        string? resultDirection,
        long count)
    {   
        SignalName = signalName;
        ResultDirection = resultDirection;
        Count = count;
    }

    public string SignalName { get; init; }
    public string? ResultDirection { get; init; }
    public long Count { get; init; }
}
