namespace Kyna.Infrastructure.Database.DataAccessObjects.Reports;

internal struct SignalCounts
{
    public Guid BacktestId;
    public string SignalName;
    public string? ResultDirection;
    public long Count;
}

internal struct SignalSummaryDetails
{
    public Guid BacktestId;
    public string Name;
    public string Category;
    public string SubCategory;
    public int NumberSignals;
    public double SuccessPercentage;
    public int SuccessDuration;
}

internal struct SignalDetails
{
    public Guid BacktestId;
    public string Name;
    public string Code;
    public string? Industry;
    public string? Sector;
    public DateOnly EntryDate;
    public string EntryPricePoint;
    public decimal EntryPrice;
    public DateOnly? ResultUpDate;
    public string? ResultUpPricePoint;
    public decimal? ResultUpPrice;
    public DateOnly? ResultDownDate;
    public string? ResultDownPricePoint;
    public decimal? ResultDownPrice;
    public string? ResultDirection;
    public int? TradingDays;
    public int? CalendarDays;
}
