using Kyna.Analysis.Technical.Charts;
using Kyna.Common;

namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class EodPrice : DaoBase
{
    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;

    public EodPrice() : this(source: "", code: "",
        dateEod: DateOnly.MinValue,
        open: 0M, high: 0M, low: 0M, close: 0M, volume: 0L)
    { }

    public EodPrice(string source, string code,
        DateOnly dateEod,
        decimal open, decimal high, decimal low, decimal close,
        long volume,
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
    }

    public EodPrice(string source, string code, Guid? processId = null) : base(processId)
    {
        Source = source;
        Code = code;
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public DateOnly DateEod { get; init; }
    public decimal Open { get => _open; init => _open = Math.Round(value, Constants.MoneyPrecision); }
    public decimal High { get => _high; init => _high = Math.Round(value, Constants.MoneyPrecision); }
    public decimal Low { get => _low; init => _low = Math.Round(value, Constants.MoneyPrecision); }
    public decimal Close { get => _close; init => _close = Math.Round(value, Constants.MoneyPrecision); }
    public long Volume { get; init; }

    public Ohlc ToOhlc() =>
        new(Code, DateEod, Open, High, Low, Close, Volume, 1D);
}

internal sealed record class EodAdjustedPrice : DaoBase
{
    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;

    public EodAdjustedPrice() : this(
        source: "",
        code: "",
        dateEod: DateOnly.MinValue,
        open: 0M, high: 0M, low: 0M, close: 0M, volume: 0L, factor: 0D)
    { }

    public EodAdjustedPrice(string source, string code,
        DateOnly dateEod,
        decimal open, decimal high, decimal low, decimal close,
        long volume,
        double factor,
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
    }

    internal EodAdjustedPrice(string source, string code, Guid? processId = null)
        : base(processId)
    {
        Source = source;
        Code = code;
    }

    public EodAdjustedPrice(EodPrice eodPrice, double factor = 1D)
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
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public DateOnly DateEod { get; init; }
    public decimal Open { get => _open; init => _open = Math.Round(value, Constants.MoneyPrecision); }
    public decimal High { get => _high; init => _high = Math.Round(value, Constants.MoneyPrecision); }
    public decimal Low { get => _low; init => _low = Math.Round(value, Constants.MoneyPrecision); }
    public decimal Close { get => _close; init => _close = Math.Round(value, Constants.MoneyPrecision); }
    public long Volume { get; init; }
    public double Factor { get; init; } = 1D;

    public Ohlc ToOhlc() =>
        new(Code, DateEod, Open, High, Low, Close, Volume, Factor);
}