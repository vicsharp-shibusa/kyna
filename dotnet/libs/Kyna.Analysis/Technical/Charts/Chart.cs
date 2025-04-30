using Kyna.Analysis.Technical.Trends;

namespace Kyna.Analysis.Technical.Charts;

public class Chart : IEquatable<Chart?>
{
    private readonly List<MovingAverage> _movingaverages = new(3);
    private readonly HashSet<MovingAverageKey> _movingAverageKeys = new(3);
    private decimal[] _averageHeights = [];
    private decimal[] _averageBodyHeights = [];
    private long[] _averageVolumes = [];
    private TrendSentiment[] _lookbackSentiment = [];
    private readonly int _lookbackLength = 15;

    public ChartInfo Info { get; init; }
    internal Chart(string name, string? source, string? industry, string? sector,
        ChartInterval interval = ChartInterval.Daily,
        int lookbackLength = 15)
    {
        Info = new()
        {
            Code = name,
            Source = source,
            Industry = industry,
            Sector = sector,
            Interval = interval
        };

        _lookbackLength = Math.Max(lookbackLength, 0);
    }

    private ITrend? Trend { get; set; } = null;
    public double[] TrendValues => Trend?.TrendValues ??
        [.. Enumerable.Repeat(0D, PriceActions.Length)];
    public Ohlc[] PriceActions { get; private set; } = [];
    public Candlestick[] Candlesticks { get; private set; } = [];
    public int Length => PriceActions.Length;
    public int Duration => End.DayNumber - Start.DayNumber;
    public DateOnly Start => PriceActions[0].Date;
    public DateOnly End => PriceActions[^1].Date;
    public MovingAverage[] MovingAverages => [.. _movingaverages];

    public Chart WithMovingAverage(MovingAverageKey key)
    {
        _movingAverageKeys.Add(key);
        return this;
    }

    public Chart WithMovingAverage(int period, MovingAverageType type, PricePoint pricePoint = PricePoint.Close)
    {
        var key = new MovingAverageKey(period, pricePoint, type);
        return WithMovingAverage(key);
    }

    public Chart WithMovingAverages(params MovingAverageKey[] keys)
    {
        foreach (var key in keys)
        {
            _movingAverageKeys.Add(key);
        }
        return this;
    }

    public Chart WithPriceActions(IEnumerable<Ohlc> priceActions)
    {
        PriceActions = [.. priceActions];
        return this;
    }

    public Chart WithCandles(IEnumerable<Ohlc> priceActions)
    {
        return WithPriceActions(priceActions);
    }

    public Chart WithTrend(ITrend trend)
    {
        Trend = trend;
        return this;
    }

    public Chart Build()
    {
        if (PriceActions.Length < 1)
        {
            throw new Exception($"Cannot construct a chart with {PriceActions.Length} price actions.");
        }

        Candlesticks = [.. PriceActions.Select(p => new Candlestick(p))];

        _movingaverages.Clear();
        foreach (var key in _movingAverageKeys)
        {
            _movingaverages.Add(new MovingAverage(key, PriceActions));
        }

        Trend?.Calculate();

        _averageHeights = new decimal[PriceActions.Length];
        _averageBodyHeights = new decimal[PriceActions.Length];
        _averageVolumes = new long[PriceActions.Length];
        _lookbackSentiment = new TrendSentiment[PriceActions.Length];

        for (int p = 0; p < PriceActions.Length; p++)
        {
            if (_lookbackLength > 0 && p > _lookbackLength)
            {
                var lookback = Candlesticks[(p - _lookbackLength - 1)..(p - 1)];
                _lookbackSentiment[p] = lookback.All(pr => pr.High < Candlesticks[p].High)
                    ? TrendSentiment.Bullish
                    : lookback.All(pr => pr.Low > Candlesticks[p].Low)
                    ? TrendSentiment.Bearish
                    : TrendSentiment.Neutral;
            }
            else
            {
                _lookbackSentiment[p] = TrendSentiment.Neutral;
            }

            if (p == 0)
            {
                _averageHeights[p] = Candlesticks[p].Length;
                _averageBodyHeights[p] = Candlesticks[p].Body.Length;
                _averageVolumes[p] = Candlesticks[p].Volume;
            }
            else
            {
                _averageHeights[p] = _averageHeights[p - 1] + (Candlesticks[p].Length - _averageHeights[p - 1]) / (p + 1);
                _averageBodyHeights[p] = _averageBodyHeights[p - 1] + (Candlesticks[p].Body.Length - _averageBodyHeights[p - 1]) / (p + 1);
                _averageVolumes[p] = Convert.ToInt64(Math.Ceiling((decimal)_averageVolumes[p - 1] + (Candlesticks[p].Volume - _averageVolumes[p - 1]) / (p + 1)));
            }
        }

        return this;
    }

    public int GetIndexOfDate(DateOnly date)
    {
        var ohlc = PriceActions.FirstOrDefault(p => p.Date.Equals(date));
        if (ohlc != null)
        {
            return Array.IndexOf(PriceActions, ohlc);
        }
        return -1;
    }

    public bool IsTall(int position, int lookbackPeriod = 0, decimal tolerance = 1M)
    {
        if (position < 10)
        {
            return false;
        }

        tolerance = Math.Max(1M, tolerance);

        var lookbackPosition = lookbackPeriod == 0 ? 0 : Math.Max(position - lookbackPeriod, 0);

        if (lookbackPosition == 0)
        {
            return Candlesticks[position].Body.Length > tolerance * _averageBodyHeights[position - 1];
        }
        else
        {
            var avg = Candlesticks[lookbackPosition..(position - 1)].Select(c => c.Body.Length).Average();
            return Candlesticks[position].Body.Length > tolerance * avg;
        }
    }

    public bool IsShort(int position, int lookbackPeriod = 0, decimal tolerance = 1M)
    {
        if (position < 10)
        {
            return false;
        }

        tolerance = Math.Min(1M, tolerance);

        var lookbackPosition = lookbackPeriod == 0 ? 0 : Math.Max(position - lookbackPeriod, 0);

        if (lookbackPeriod == 0)
        {
            return Candlesticks[position].Body.Length < tolerance * _averageBodyHeights[position - 1];
        }
        else
        {
            var avg = Candlesticks[lookbackPosition..(position - 1)].Select(c => c.Body.Length).Average();
            return Candlesticks[position].Body.Length < tolerance * avg;
        }
    }

    public TrendSentiment LookbackSentiment(int position) => position > -1 && position < _lookbackSentiment.Length
        ? _lookbackSentiment[position]
        : TrendSentiment.Neutral;

    public override bool Equals(object? obj)
    {
        return Equals(obj as Chart);
    }

    public bool Equals(Chart? other)
    {
        return other is not null &&
               _lookbackLength == other._lookbackLength &&
               Info.Interval == other.Info.Interval &&
               Info.Source == other.Info.Source &&
               Info.Code == other.Info.Code &&
               Info.Industry == other.Info.Industry &&
               Info.Sector == other.Info.Sector &&
               Length == other.Length &&
               Start.Equals(other.Start) &&
               End.Equals(other.End);
    }

    public static int GetCacheKey(ChartInfo chartInfo,
        string? trend = null, int lookbackLength = 15)
    {
        return HashCode.Combine(chartInfo, trend, lookbackLength);
    }

    public override int GetHashCode()
    {
        return GetCacheKey(Info, Trend?.Name, _lookbackLength);
    }
}