namespace Kyna.Common;

public struct ProcessIdInfo
{
    public Guid ProcessId;
    public int BacktestCount;
    public DateTime MinDate;
    public DateTime MaxDate;

    public override readonly string ToString() =>
        $"{ProcessId} | {BacktestCount,4} | {MinDate:yyyy-MM-dd HH:mm} | {MaxDate:yyyy-MM-dd HH:mm}";
}
