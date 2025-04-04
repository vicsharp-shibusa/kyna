﻿using Kyna.Analysis.Technical.Charts;

namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class EodPrice : DaoBase
{
    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;

    public EodPrice(string source, string code,
        DateOnly dateEod,
        decimal open, decimal high, decimal low, decimal close,
        long volume,
        long createdTicksUtc, long updatedTicksUtc,
        Guid? processId = null) : base(processId)
    {
        Source = source;
        Code = code;
        DateEod = dateEod;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        CreatedTicksUtc = createdTicksUtc;
        UpdatedTicksUtc = updatedTicksUtc;
    }

    public EodPrice(string source, string code, Guid? processId = null)
        : base(processId)
    {
        Source = source;
        Code = code;
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public DateOnly DateEod { get; init; }
    public decimal Open { get => _open; init => _open = Math.Round(value, MoneyPrecision); }
    public decimal High { get => _high; init => _high = Math.Round(value, MoneyPrecision); }
    public decimal Low { get => _low; init => _low = Math.Round(value, MoneyPrecision); }
    public decimal Close { get => _close; init => _close = Math.Round(value, MoneyPrecision); }
    public long Volume { get; init; }

    public Ohlc ToOhlc() =>
        new(Code, DateEod, Open, High, Low, Close, Volume, 1D);
}

internal sealed record class AdjustedEodPrice : DaoBase
{
    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;
    
    public AdjustedEodPrice(string source, string code,
        DateOnly dateEod,
        decimal open, decimal high, decimal low, decimal close,
        long volume,
        double factor,
        long createdTicksUtc, long updatedTicksUtc,
        Guid? processId = null) : base(processId)
    {
        Source = source;
        Code = code;
        DateEod = dateEod;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        Factor = factor;
        CreatedTicksUtc = createdTicksUtc;
        UpdatedTicksUtc = updatedTicksUtc;
    }

    internal AdjustedEodPrice(string source, string code, Guid? processId = null)
        : base(processId)
    {
        Source= source;
        Code = code;
    }

    public AdjustedEodPrice(EodPrice eodPrice, double factor = 1D)
        : base(eodPrice.ProcessId)
    {
        Source = eodPrice.Source;
        Code = eodPrice.Code;
        DateEod = eodPrice.DateEod;
        Open = eodPrice.Open / (decimal)factor;
        High = eodPrice.High / (decimal)factor;
        Low = eodPrice.Low / (decimal)factor;
        Close = eodPrice.Close / (decimal)factor;
        Volume = Convert.ToInt64(eodPrice.Volume * factor);
        Factor = factor;
        CreatedTicksUtc = eodPrice.CreatedTicksUtc;
        UpdatedTicksUtc = eodPrice.UpdatedTicksUtc;
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public DateOnly DateEod { get; init; }
    public decimal Open { get => _open; init => _open = Math.Round(value, MoneyPrecision); }
    public decimal High { get => _high; init => _high = Math.Round(value, MoneyPrecision); }
    public decimal Low { get => _low; init => _low = Math.Round(value, MoneyPrecision); }
    public decimal Close { get => _close; init => _close = Math.Round(value, MoneyPrecision); }
    public long Volume { get; init; }
    public double Factor { get; init; } = 1D;

    public Ohlc ToOhlc() =>
        new(Code, DateEod, Open, High, Low, Close, Volume, Factor);
}