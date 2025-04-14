using Kyna.Analysis.Technical.Charts;
using Kyna.Analysis.Technical.Trends;
using Kyna.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

            return new MovingAverageKey(Convert.ToInt32(match.Groups[2].Value), pricePoint, type);
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
                trend = new CombinedWeightedTrend([.. wt]);
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

        var ts = ohlc.End - ohlc.Start;

        return ts.TotalDays switch
        {
            > 95 => ChartInterval.Annually,
            > 35 => ChartInterval.Quarterly,
            > 27 => ChartInterval.Monthly,
            _ => ChartInterval.Weekly
        };
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

        public static Ohlc[] ToWeekly(Ohlc[] ohlc) => Aggregate(ohlc, IsNewWeek, ohlc.Length / 5);
        public static Ohlc[] ToMonthly(Ohlc[] ohlc) => Aggregate(ohlc, IsNewMonth, ohlc.Length / 30);
        public static Ohlc[] ToQuarterly(Ohlc[] ohlc) => Aggregate(ohlc, IsNewQuarter, ohlc.Length / 90);
        public static Ohlc[] ToAnnually(Ohlc[] ohlc) => Aggregate(ohlc, IsNewYear, ohlc.Length / 360);

        private static Ohlc[] Aggregate(Ohlc[] ohlc, Func<Ohlc, Ohlc, bool> isNewPeriod, int initialCapacity)
        {
            ArgumentNullException.ThrowIfNull(ohlc, nameof(ohlc));
            if (ohlc.Length == 0)
            {
                return [];
            }

            List<Ohlc> results = new(initialCapacity);
            Candle? candle = null;

            for (int i = 0; i < ohlc.Length; i++)
            {
                if (i > 0 && ohlc[i] < ohlc[i - 1])
                {
                    throw new ArgumentException("OHLC data must be in chronological order.", nameof(ohlc));
                }

                bool isLast = i == ohlc.Length - 1;
                bool startNewPeriod = candle != null && i > 0 && isNewPeriod(ohlc[i - 1], ohlc[i]);

                if (startNewPeriod)
                {
                    results.Add(candle!.ToOhlc());
                    candle = null;
                }

                if (candle == null)
                {
                    candle = CreateCandle(ohlc[i]);
                }
                else
                {
                    UpdateCandle(candle, ohlc[i]);
                }

                if (isLast)
                {
                    results.Add(candle.ToOhlc());
                }
            }

            return [.. results];
        }

        private static Candle CreateCandle(Ohlc ohlc) => new()
        {
            Symbol = ohlc.Symbol,
            Start = ohlc.Date,
            End = ohlc.Date,
            Open = ohlc.Open,
            High = ohlc.High,
            Low = ohlc.Low,
            Close = ohlc.Close,
            Volume = ohlc.Volume
        };

        private static void UpdateCandle(Candle candle, Ohlc ohlc)
        {
            candle.End = ohlc.Date;
            candle.Close = ohlc.Close;
            candle.High = Math.Max(candle.High, ohlc.High);
            candle.Low = Math.Min(candle.Low, ohlc.Low);
            candle.Volume += ohlc.Volume;
        }

        private static bool IsNewWeek(Ohlc previous, Ohlc current) =>
            current.Date.DayOfWeek < previous.Date.DayOfWeek;

        private static bool IsNewMonth(Ohlc previous, Ohlc current) =>
            current > previous && current.Date.Month != previous.Date.Month;

        private static bool IsNewQuarter(Ohlc previous, Ohlc current)
        {
            if (current <= previous)
                return false;

            int prevQuarter = (previous.Date.Month - 1) / 3 + 1;
            int currQuarter = (current.Date.Month - 1) / 3 + 1;
            return currQuarter != prevQuarter;
        }

        private static bool IsNewYear(Ohlc previous, Ohlc current) =>
            current > previous && current.Date.Year != previous.Date.Year;
    }
}