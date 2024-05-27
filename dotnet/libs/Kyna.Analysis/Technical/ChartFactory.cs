using Kyna.Analysis.Technical.Trends;
using Kyna.Common;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace Kyna.Analysis.Technical;

public sealed class ChartConfiguration
{
    public string? Interval { get; init; }

    [JsonPropertyName("Moving Averages")]
    public string[]? MovingAverages { get; init; }

    [JsonPropertyName("Trend Configuration")]
    public TrendConfiguration[]? Trends { get; init; }
    [JsonPropertyName("Length of Prologue")]
    public int LengthOfPrologue { get; init; } = 15;
}

public sealed class MarketConfiguration
{
    public string[]? Codes { get; init; }

    [JsonPropertyName("Trend Configuration")]
    public TrendConfiguration[]? Trends { get; init; }
}

public sealed class TrendConfiguration
{
    public string? Trend { get; init; }
    public double? Weight { get; init; }
}

internal static partial class ConvertMatch
{
    [GeneratedRegex(@"([SE])(\d+)([OHLCM])", RegexOptions.Singleline)]
    private static partial Regex MaRegex();

    public readonly static Regex MovingAverageRegex = MaRegex();

    public static MovingAverageKey? ToMovingAverageKey(Match match)
    {
        if (match.Success)
        {
            var pricePoint = match.Groups[3].Value switch
            {
                "O" => PricePoint.Open,
                "H" => PricePoint.High,
                "L" => PricePoint.Low,
                "C" => PricePoint.Close,
                "M" => PricePoint.MidPoint,
                _ => throw new Exception($"Could not parse '{match.Groups[3].Value}' into a price point.")
            };

            var type = match.Groups[1].Value switch
            {
                "S" => MovingAverageType.Simple,
                "E" => MovingAverageType.Exponential,
                _ => throw new Exception($"Could not parse '{match.Groups[1].Value}' into a moving average type.")
            };

            return new MovingAverageKey(Convert.ToInt32(match.Groups[2].Value),
                pricePoint, type);
        }
        return null;
    }
}

public static class ChartFactory
{
    private readonly static MemoryCache _memoryCache = new(new MemoryCacheOptions()
    {
        ExpirationScanFrequency = TimeSpan.FromHours(2),
    });

    public static Chart Create(string source, string name, ChartConfiguration? configuration, string? industry, string? sector,
        params Ohlc[][] ohlcs)
    {
        if (ohlcs.Length == 0)
        {
            throw new ArgumentException("A chart cannot be created with 0 OHLCs");
        }

        foreach (var ohlc in ohlcs)
        {
            if (ohlc.Length == 0)
            {
                throw new ArgumentException("A chart cannot be created with empty OHLC arrays.");
            }
        }

        var dates = ohlcs.SelectMany(n => n.Select(o => o.Date)).Distinct().OrderBy(d => d).ToArray();

        Ohlc[] combinedOhlc = new Ohlc[dates.Length];

        int i = 0;
        foreach (var date in dates)
        {
            var sameDateOhlcs = new List<Ohlc>(ohlcs.Length);
            foreach (var arr in ohlcs)
            {
                var ohlc = arr.FirstOrDefault(o => o.Date.Equals(date));
                if (ohlc != null)
                {
                    sameDateOhlcs.Add(ohlc);
                }
            }
            if (sameDateOhlcs.Count != 0)
            {
                combinedOhlc[i++] = CombineOhlcs(name, date, [.. sameDateOhlcs]);
            }
        }

        return Create(source, name, industry, sector, combinedOhlc, configuration);
    }

    private static Ohlc CombineOhlcs(string name, DateOnly date, Ohlc[] ohlcs)
    {
        return new Ohlc(name, date,
            ohlcs.Select(a => a.Open).Average(),
            ohlcs.Select(a => a.High).Average(),
            ohlcs.Select(a => a.Low).Average(),
            ohlcs.Select(a => a.Close).Average(),
            Convert.ToInt64(Math.Ceiling(ohlcs.Select(a => a.Volume).Average())));
    }

    public static Chart Create(string? source, string? code, string? industry, string? sector,
        Ohlc[] ohlc, ChartConfiguration? configuration)
    {
        if (ohlc.Length == 0)
        {
            throw new ArgumentException("A chart cannot be created with 0 OHLCs");
        }

        ChartInterval interval = DetermineInterval(ohlc[0]);
        List<MovingAverageKey> movingAverageKeys = new(3);
        ITrend? trend = null;

        if (configuration != null)
        {
            if (!string.IsNullOrWhiteSpace(configuration.Interval))
            {
                var expectedInterval = configuration.Interval.GetEnumValueFromDescription<ChartInterval>();

                if (interval != expectedInterval && interval == ChartInterval.Daily)
                {
                    ohlc = expectedInterval switch
                    {
                        ChartInterval.Weekly => ConvertFromDaily.ToWeekly(ohlc),
                        ChartInterval.Monthly => ConvertFromDaily.ToMonthly(ohlc),
                        ChartInterval.Quarterly => ConvertFromDaily.ToQuarterly(ohlc),
                        ChartInterval.Annually => ConvertFromDaily.ToAnnually(ohlc),
                        _ => throw new Exception($"Cannot convert chart to {expectedInterval.GetEnumDescription()}")
                    };
                    interval = expectedInterval;
                }
            }

            if (configuration.MovingAverages != null)
            {
                foreach (var mak in configuration.MovingAverages.Select(m => m.ToUpper()))
                {
                    var key = ConvertMatch.ToMovingAverageKey(ConvertMatch.MovingAverageRegex.Match(mak));
                    if (key != null)
                    {
                        movingAverageKeys.Add(key.Value);
                    }
                }
            }

            var len = configuration.Trends?.Length ?? 0;
            if (len == 1 && !string.IsNullOrWhiteSpace(configuration.Trends![0].Trend))
            {
                var key = ConvertMatch.ToMovingAverageKey(ConvertMatch.MovingAverageRegex.Match(configuration.Trends[0].Trend!));
                if (key != null)
                {
                    trend = new MovingAverageTrend(key.Value, ohlc);
                }
                else if (configuration.Trends![0].Trend!
                    .StartsWith("Extreme", StringComparison.OrdinalIgnoreCase))
                {
                    trend = new ExtremeTrend(ohlc);
                }
            }
            if (len > 1)
            {
                List<WeightedTrend> wt = new(len);
                const double DefaultWeight = 1;

                foreach (var configTrend in configuration.Trends!.Where(t => t.Trend != null))
                {
                    ITrend tr;

                    var key = ConvertMatch.ToMovingAverageKey(ConvertMatch.MovingAverageRegex.Match(configTrend.Trend!));
                    if (key != null)
                    {
                        tr = new MovingAverageTrend(key.Value, ohlc);
                        wt.Add(new WeightedTrend(tr, configTrend.Weight ?? DefaultWeight));
                    }
                    else if (configTrend.Trend!.StartsWith("Extreme", StringComparison.OrdinalIgnoreCase))
                    {
                        tr = new ExtremeTrend(ohlc);
                        wt.Add(new WeightedTrend(tr, configTrend.Weight ?? DefaultWeight));
                    }
                }
                trend = new CombinedTrend([.. wt]);
            }
        }
        else
        {
            configuration = new ChartConfiguration()
            {
                Interval = interval.GetEnumDescription(),
                Trends = [
                    new TrendConfiguration(){
                        Trend = "S200C"
                    }
                ],
                LengthOfPrologue = 15
            };
        }

        if (_memoryCache.TryGetValue(Chart.GetCacheKey(source, code, industry, sector, trend?.Name,
            configuration.LengthOfPrologue, interval), out Chart? chart) && chart != null)
        {
            return chart;
        }

        chart = new Chart(source, code, industry, sector, interval, configuration.LengthOfPrologue).WithCandles(ohlc);

        if (movingAverageKeys.Count != 0)
        {
            chart = chart.WithMovingAverages([.. movingAverageKeys]);
        }

        if (trend != null)
        {
            chart = chart.WithTrend(trend);
        }

        chart = chart.Build();

        _memoryCache.Set(chart.GetHashCode(), chart, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
        });

        return chart;
    }

    private static ChartInterval DetermineInterval(Ohlc ohlc)
    {
        if (DateOnly.FromDateTime(ohlc.Start).Equals(DateOnly.FromDateTime(ohlc.End)))
        {
            return ChartInterval.Daily;
        }

        var ts = new TimeSpan(ohlc.End.Ticks - ohlc.Start.Ticks);

        if (ts.TotalDays > 95)
        {
            return ChartInterval.Annually;
        }

        if (ts.TotalDays > 35)
        {
            return ChartInterval.Quarterly;
        }

        if (ts.TotalDays > 27)
        {
            return ChartInterval.Monthly;
        }

        return ChartInterval.Weekly;
    }

    public static class ConvertFromDaily
    {
        private class Candle
        {
            public string Symbol { get; set; } = "";
            public DateOnly Start { get; set; }
            public DateOnly End { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public long Volume { get; set; }

            public Ohlc ToOhlc() => new(Symbol, Start.ToDateTime(TimeOnly.MinValue),
                End.ToDateTime(TimeOnly.MaxValue), Open, High, Low, Close, Volume);
        }

        public static Ohlc[] ToWeekly(Ohlc[] ohlc)
        {
            if (ohlc.Length == 0)
            {
                return [];
            }

            List<Ohlc> results = new(1 + ohlc.Length / 5);

            Candle? candle = null;

            for (int i = 0; i < ohlc.Length; i++)
            {
                if (i == ohlc.Length - 1)
                {
                    if (candle == null)
                    {
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else
                    {
                        candle.End = ohlc[i].Date;
                        candle.Close = ohlc[i].Close;
                        if (ohlc[i].Low < candle.Low)
                        {
                            candle.Low = ohlc[i].Low;
                        }
                        if (ohlc[i].High > candle.High)
                        {
                            candle.High = ohlc[i].High;
                        }
                        candle.Volume += ohlc[i].Volume;
                    }
                    results.Add(candle.ToOhlc());
                }
                else if (candle == null)
                {
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else if (ohlc[i].Date.DayOfWeek > DayOfWeek.Tuesday &&
                    ohlc[i + 1].Date.DayOfWeek < DayOfWeek.Wednesday)
                {
                    candle.End = ohlc[i].Date;
                    candle.Close = ohlc[i].Close;
                    if (ohlc[i].Low < candle.Low)
                    {
                        candle.Low = ohlc[i].Low;
                    }
                    if (ohlc[i].High > candle.High)
                    {
                        candle.High = ohlc[i].High;
                    }
                    candle.Volume += ohlc[i].Volume;
                    results.Add(candle.ToOhlc());
                    candle = null;
                    continue;
                }
                else
                {
                    candle.End = ohlc[i].Date;
                    candle.Close = ohlc[i].Close;
                    if (ohlc[i].Low < candle.Low)
                    {
                        candle.Low = ohlc[i].Low;
                    }
                    if (ohlc[i].High > candle.High)
                    {
                        candle.High = ohlc[i].High;
                    }
                    candle.Volume += ohlc[i].Volume;
                }
            }
            return [.. results];
        }

        public static Ohlc[] ToMonthly(Ohlc[] ohlc)
        {
            if (ohlc.Length == 0)
            {
                return [];
            }

            List<Ohlc> results = new(ohlc.Length / 30);

            Candle? candle = null;
            var m = ohlc[0].Date.Month;
            for (int i = 0; i < ohlc.Length; i++)
            {
                if (i == ohlc.Length - 1)
                {
                    if (candle == null)
                    {
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else if (ohlc[i].Date.Month != m)
                    {
                        results.Add(candle.ToOhlc());
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else
                    {
                        candle.End = ohlc[i].Date;
                        candle.Close = ohlc[i].Close;
                        if (ohlc[i].Low < candle.Low)
                        {
                            candle.Low = ohlc[i].Low;
                        }
                        if (ohlc[i].High > candle.High)
                        {
                            candle.High = ohlc[i].High;
                        }
                        candle.Volume += ohlc[i].Volume;
                    }
                    results.Add(candle.ToOhlc());
                }
                else if (candle == null)
                {
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else if (ohlc[i].Date.Month != m)
                {
                    results.Add(candle.ToOhlc());
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else
                {
                    candle.End = ohlc[i].Date;
                    candle.Close = ohlc[i].Close;
                    if (ohlc[i].Low < candle.Low)
                    {
                        candle.Low = ohlc[i].Low;
                    }
                    if (ohlc[i].High > candle.High)
                    {
                        candle.High = ohlc[i].High;
                    }
                    candle.Volume += ohlc[i].Volume;
                }
                m = ohlc[i].Date.Month;
            }

            return [.. results];
        }

        public static Ohlc[] ToQuarterly(Ohlc[] ohlc)
        {
            if (ohlc.Length == 0)
            {
                return [];
            }

            List<Ohlc> results = new(ohlc.Length / 90);

            Candle? candle = null;
            var m = ohlc[0].Date.Year;
            for (int i = 0; i < ohlc.Length; i++)
            {
                if (i == ohlc.Length - 1)
                {
                    if (candle == null)
                    {
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else if (IsNewQuarter(m, ohlc[i].Date.Month))
                    {
                        results.Add(candle.ToOhlc());
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else
                    {
                        candle.End = ohlc[i].Date;
                        candle.Close = ohlc[i].Close;
                        if (ohlc[i].Low < candle.Low)
                        {
                            candle.Low = ohlc[i].Low;
                        }
                        if (ohlc[i].High > candle.High)
                        {
                            candle.High = ohlc[i].High;
                        }
                        candle.Volume += ohlc[i].Volume;
                    }
                    results.Add(candle.ToOhlc());
                }
                else if (candle == null)
                {
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else if (IsNewQuarter(m, ohlc[i].Date.Month))
                {
                    results.Add(candle.ToOhlc());
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else
                {
                    candle.End = ohlc[i].Date;
                    candle.Close = ohlc[i].Close;
                    if (ohlc[i].Low < candle.Low)
                    {
                        candle.Low = ohlc[i].Low;
                    }
                    if (ohlc[i].High > candle.High)
                    {
                        candle.High = ohlc[i].High;
                    }
                    candle.Volume += ohlc[i].Volume;
                }
                m = ohlc[i].Date.Month;
            }

            return [.. results];
        }

        public static Ohlc[] ToAnnually(Ohlc[] ohlc)
        {
            if (ohlc.Length == 0)
            {
                return [];
            }

            List<Ohlc> results = new(ohlc.Length / 360);

            Candle? candle = null;
            var y = ohlc[0].Date.Year;
            for (int i = 0; i < ohlc.Length; i++)
            {
                if (i == ohlc.Length - 1)
                {
                    if (candle == null)
                    {
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else if (ohlc[i].Date.Year != y)
                    {
                        results.Add(candle.ToOhlc());
                        candle = new Candle()
                        {
                            Start = ohlc[i].Date,
                            End = ohlc[i].Date,
                            Open = ohlc[i].Open,
                            High = ohlc[i].High,
                            Low = ohlc[i].Low,
                            Close = ohlc[i].Close,
                            Volume = ohlc[i].Volume
                        };
                    }
                    else
                    {
                        candle.End = ohlc[i].Date;
                        candle.Close = ohlc[i].Close;
                        if (ohlc[i].Low < candle.Low)
                        {
                            candle.Low = ohlc[i].Low;
                        }
                        if (ohlc[i].High > candle.High)
                        {
                            candle.High = ohlc[i].High;
                        }
                        candle.Volume += ohlc[i].Volume;
                    }
                    results.Add(candle.ToOhlc());
                }
                else if (candle == null)
                {
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else if (ohlc[i].Date.Year != y)
                {
                    results.Add(candle.ToOhlc());
                    candle = new Candle()
                    {
                        Start = ohlc[i].Date,
                        End = ohlc[i].Date,
                        Open = ohlc[i].Open,
                        High = ohlc[i].High,
                        Low = ohlc[i].Low,
                        Close = ohlc[i].Close,
                        Volume = ohlc[i].Volume
                    };
                }
                else
                {
                    candle.End = ohlc[i].Date;
                    candle.Close = ohlc[i].Close;
                    if (ohlc[i].Low < candle.Low)
                    {
                        candle.Low = ohlc[i].Low;
                    }
                    if (ohlc[i].High > candle.High)
                    {
                        candle.High = ohlc[i].High;
                    }
                    candle.Volume += ohlc[i].Volume;
                }
                y = ohlc[i].Date.Year;
            }

            return [.. results];
        }

        private static bool IsNewQuarter(int previousMonth, int currentMonth) =>
            (previousMonth is 1 or 2 or 3 && currentMonth == 4) ||
            (previousMonth is 4 or 5 or 6 && currentMonth == 7) ||
            (previousMonth is 7 or 8 or 9 && currentMonth == 10) ||
            (previousMonth is 10 or 11 or 12 && currentMonth == 1);
    }
}