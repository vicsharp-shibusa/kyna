﻿using Kyna.Common;

namespace Kyna.Analysis.Technical.Charts;

public record class Ohlc : PriceRange
{
    public Ohlc(string symbol, DateTime start, DateTime end,
        decimal open, decimal high, decimal low, decimal close, long volume, double factor = 1D)
    : base(high, low)
    {
        Symbol = symbol;
        Date = DateOnly.FromDateTime(start);
        Start = start;
        End = end;
        Open = Math.Round(open, Constants.MoneyPrecision);
        Close = Math.Round(close, Constants.MoneyPrecision);
        Volume = volume;
        Factor = factor;
    }

    public Ohlc(string symbol, DateOnly date, decimal open, decimal high, decimal low, decimal close, long volume,
        double factor = 1D)
        : this(symbol, date.ToDateTime(TimeOnly.MinValue), date.ToDateTime(TimeOnly.MaxValue),
              open, high, low, close, volume, factor)
    {
    }

    public string Symbol { get; }
    public DateOnly Date { get; }
    public DateTime Start { get; }
    public DateTime End { get; }
    public decimal Open { get; }
    public decimal Close { get; }
    public long Volume { get; }
    public double Factor { get; }
    public decimal AveragePrice => (Open + High + Low + Close) / 4M;
    public decimal Liquidity => Close * Volume;
    public bool IsLight => Close > Open;
    public bool IsDark => Open > Close;
    public bool IsFlat => Open == Close;

    public decimal GetPricePoint(PricePoint pricePoint)
    {
        return pricePoint switch
        {
            PricePoint.Open => Open,
            PricePoint.Close => Close,
            PricePoint.Low => Low,
            PricePoint.High => High,
            _ => MidPoint
        };
    }

    public static bool operator <(Ohlc a, Ohlc b) =>
        a.Date.DayNumber < b.Date.DayNumber;

    public static bool operator >(Ohlc a, Ohlc b) =>
        a.Date.DayNumber > b.Date.DayNumber;

    public static bool operator <=(Ohlc a, Ohlc b) =>
        a.Date.DayNumber <= b.Date.DayNumber;

    public static bool operator >=(Ohlc a, Ohlc b) =>
        a.Date.DayNumber >= b.Date.DayNumber;
}
