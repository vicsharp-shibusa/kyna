using Kyna.Analysis.Technical;
using Kyna.Common;

namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Backtest : DaoBase
{
    public Backtest() : this(id: Guid.NewGuid(),
        name: "",
        type: "",
        source: "",
        description: "",
        entryPricePoint: "",
        targetUpPercentage: 0D,
        targetUpPricePoint: PricePoint.Close.GetEnumDescription(),
        targetDownPercentage: 0D,
        targetDownPricePoint: PricePoint.Close.GetEnumDescription())
    { }

    public Backtest(Guid id, string name, string type, string source, string description,
        string entryPricePoint,
        double targetUpPercentage, string targetUpPricePoint,
        double targetDownPercentage, string targetDownPricePoint,
        Guid? processId = null) : base(processId)
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
    public BacktestResult() : this(id: Guid.Empty,
        backtestId: Guid.Empty,
        signalName: "",
        code: "",
        industry: null, sector: null,
        entryDate: DateOnly.MinValue,
        entryPricePoint: "",
        entryPrice: 0M,
        resultUpDate: null,
        resultUpPricePoint: null,
        resultUpPrice: null,
        resultDownDate: null,
        resultDownPricePoint: null,
        resultDownPrice: null,
        resultDirection: null,
        resultDurationTradingDays: null,
        resultDurationCalendarDays: null)
    {
    }

    public BacktestResult(Guid id, Guid backtestId, string signalName, string code, string? industry, string? sector,
        DateOnly entryDate, string entryPricePoint, decimal entryPrice,
        DateOnly? resultUpDate, string? resultUpPricePoint, decimal? resultUpPrice,
        DateOnly? resultDownDate, string? resultDownPricePoint, decimal? resultDownPrice,
        string? resultDirection, int? resultDurationTradingDays, int? resultDurationCalendarDays)
        : base((Guid?)null)
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
    public BacktestStats() : this(backtestId: Guid.Empty,
        source: "",
        signalName: "",
        category: "",
        subCategory: "",
        numberEntities: 0,
        numberSignals: 0,
        successPercentage: 0D,
        successCriterion: "",
        successDurationTradingDays: null,
        successDurationCalendarDays: null,
        processId: null)
    { }

    public BacktestStats(
        Guid backtestId,
        string source,
        string signalName,
        string category,
        string subCategory,
        int numberEntities,
        int numberSignals,
        double successPercentage,
        string successCriterion,
        int? successDurationTradingDays,
        int? successDurationCalendarDays,
        Guid? processId = null) : base(processId)
    {
        BacktestId = backtestId;
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
    }

    public Guid BacktestId { get; init; }
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