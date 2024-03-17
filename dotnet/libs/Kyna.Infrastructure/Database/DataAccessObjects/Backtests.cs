namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Backtest : DaoBase
{
    public Backtest(Guid id, string name, string type, string source, string description,
        string entryPricePoint,
        double targetUpPercentage, string targetUpPricePoint,
        double targetDownPercentage, string targetDownPricePoint,
        long createdTicksUtc, long updatedTicksUtc, Guid? processId = null) : base(processId)
    {
        Id = id;
        Name = name;
        Type = type;
        Source = source;
        Description = description;
        EntryPricePoint = entryPricePoint;
        TargetUpPercentage = targetUpPercentage;
        TargetUpPricePoint = targetUpPricePoint;
        TargetDownPercentage = targetDownPercentage;
        TargetDownPricePoint = targetDownPricePoint;
        CreatedTicksUtc = createdTicksUtc;
        UpdatedTicksUtc = updatedTicksUtc;
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Type { get; init; }
    public string Source { get; init; }
    public string Description { get; init; }
    public string EntryPricePoint { get; init; }
    public double TargetUpPercentage { get; init; }
    public string TargetUpPricePoint { get; init; }
    public double TargetDownPercentage { get; init; }
    public string TargetDownPricePoint { get; init; }
}

internal sealed record class BacktestResult : DaoBase
{
    public BacktestResult(Guid id, Guid backtestId, string signalName, string code, string? industry, string? sector,
        DateOnly entryDate, string entryPricePoint, decimal entryPrice,
        DateOnly? resultUpDate, string? resultUpPricePoint, decimal? resultUpPrice,
        DateOnly? resultDownDate, string? resultDownPricePoint, decimal? resultDownPrice,
        string? resultDirection, int? resultDurationTradingDays, int? resultDurationCalendarDays,
        long createdTicksUtc, long updatedTicksUtc) : base((Guid?)null)
    {
        Id = id;
        BacktestId = backtestId;
        SignalName = signalName;
        Code = code;
        Industry = industry;
        Sector = sector;
        EntryDate = entryDate;
        EntryPricePoint = entryPricePoint;
        EntryPrice = entryPrice;
        ResultUpDate = resultUpDate;
        ResultUpPricePoint = resultUpPricePoint;
        ResultUpPrice = resultUpPrice;
        ResultDownDate = resultDownDate;
        ResultDownPricePoint = resultDownPricePoint;
        ResultDownPrice = resultDownPrice;
        ResultDirection = resultDirection;
        ResultDurationTradingDays = resultDurationTradingDays;
        ResultDurationCalendarDays = resultDurationCalendarDays;
        CreatedTicksUtc = createdTicksUtc;
        UpdatedTicksUtc = updatedTicksUtc;
    }

    public Guid Id { get; init; }
    public Guid BacktestId { get; init; }
    public string SignalName { get; init; }
    public string Code { get; init; }
    public string? Industry { get; init; }
    public string? Sector { get; init; }
    public DateOnly EntryDate { get; init; }
    public string EntryPricePoint { get; init; }
    public decimal EntryPrice { get; init; }
    public DateOnly? ResultUpDate { get; init; }
    public string? ResultUpPricePoint { get; init; }
    public decimal? ResultUpPrice { get; init; }
    public DateOnly? ResultDownDate { get; init; }
    public string? ResultDownPricePoint { get; init; }
    public decimal? ResultDownPrice { get; init; }
    public string? ResultDirection { get; init; }
    public int? ResultDurationTradingDays { get; init; }
    public int? ResultDurationCalendarDays { get; init; }
}

internal sealed record class BacktestStats : DaoBase
{
    public BacktestStats(string source, 
        string signalName, 
        string category, 
        string subCategory, 
        int numberEntities, 
        int numberSignals, 
        double successPercentage, 
        string successCriterion, 
        int? successDurationTradingDays, 
        int? successDurationCalendarDays,
        long createdTicksUtc, 
        long updatedTicksUtc,
        Guid? processId = null) : base(processId)
    {
        Source = source;
        SignalName = signalName;
        Category = category;
        SubCategory = subCategory;
        NumberEntities = numberEntities;
        NumberSignals = numberSignals;
        SuccessPercentage = successPercentage;
        SuccessCriterion = successCriterion;
        SuccessDurationTradingDays = successDurationTradingDays;
        SuccessDurationCalendarDays = successDurationCalendarDays;
        CreatedTicksUtc = createdTicksUtc;
        UpdatedTicksUtc = updatedTicksUtc;
    }

    public string Source { get; init; }
    public string SignalName { get; init; }
    public string Category { get; init; }
    public string SubCategory { get; init; }
    public int NumberEntities { get; init; }
    public int NumberSignals { get; init; }
    public double SuccessPercentage { get; init; }
    public string SuccessCriterion { get; init; }
    public int? SuccessDurationTradingDays { get; init; }
    public int? SuccessDurationCalendarDays { get; init; }
}

internal sealed record class BacktestResultsInfo
{
    public BacktestResultsInfo(string signalName, string code,
        string? industry, string? sector, 
        string? resultDirection, int? resultDurationTradingDays, int? resultDurationCalendarDays)
    {
        SignalName = signalName;
        Code = code;
        Industry = industry;
        Sector = sector;
        ResultDirection = resultDirection;
        ResultDurationTradingDays = resultDurationTradingDays;
        ResultDurationCalendarDays = resultDurationCalendarDays;
    }

    public string SignalName { get; init; }
    public string Code { get; init; }
    public string? Industry { get; init; }
    public string? Sector { get; init; }
    public string? ResultDirection { get; init; }
    public int? ResultDurationTradingDays { get; init; }
    public int? ResultDurationCalendarDays { get; init; }
}