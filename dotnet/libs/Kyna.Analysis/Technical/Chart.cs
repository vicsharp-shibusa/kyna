using Kyna.Analysis.Technical.Trends;

namespace Kyna.Analysis.Technical;

public class Chart : IEquatable<Chart?>
{
    private readonly List<MovingAverage> _movingaverages = new(3);
    private readonly HashSet<MovingAverageKey> _movingAverageKeys = new(3);
    private decimal[] _averageHeights = [];
    private decimal[] _averageBodyHeights = [];
    private long[] _averageVolumes = [];
    private TrendSentiment[] _prologueSentiment = [];
    private readonly int _prologueLength = 15;

    internal Chart(string? source, string? name, string? industry, string? sector,
        ChartInterval interval = ChartInterval.Daily,
        int prologueLength = 15)
    {
        Source = source;
        Code = name;
        Industry = industry;
        Sector = sector;
        Interval = interval;
        _prologueLength = Math.Max(prologueLength, 0);
    }

    public ChartInterval Interval { get; }
    public string? Source { get; }
    public string? Code { get; }
    public string? Industry { get; }
    public string? Sector { get; }
    private ITrend? Trend { get; set; } = null;
    public TrendValue[] TrendValues => Trend?.TrendValues ??
        Enumerable.Repeat(new TrendValue(TrendSentiment.None, 0D), PriceActions.Length).ToArray();
    public Ohlc[] PriceActions { get; private set; } = [];
    public Candlestick[] Candlesticks { get; private set; } = [];
    public int Length => PriceActions.Length;
    public DateOnly Start => PriceActions[0].Date;
    public DateOnly End => PriceActions[^1].Date;
    public MovingAverage[] MovingAverages => [.. _movingaverages];

    public int? GetIndexOfDate(DateOnly date)
    {
        var ohlc = PriceActions.FirstOrDefault(p => p.Date.Equals(date));
        if (ohlc != null)
        {
            return Array.IndexOf(PriceActions, ohlc);
        }
        return null;
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
            return Candlesticks[position].Body.Low > (tolerance * _averageBodyHeights[position - 1]);
        }
        else
        {
            var avg = Candlesticks[lookbackPosition..(position - 1)].Select(c => c.Body.Length).Average();
            return Candlesticks[position].Body.Length > (tolerance * avg);
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
            return Candlesticks[position].Body.Low < (tolerance * _averageBodyHeights[position - 1]);
        }
        else
        {
            var avg = Candlesticks[lookbackPosition..(position - 1)].Select(c => c.Body.Length).Average();
            return Candlesticks[position].Body.Length < (tolerance * avg);
        }
    }

    public TrendSentiment PrologueSentiment(int position) => _prologueSentiment[position];

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
        PriceActions = priceActions.ToArray();
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

        Candlesticks = PriceActions.Select(p => new Candlestick(p)).ToArray();

        _movingaverages.Clear();
        foreach (var key in _movingAverageKeys)
        {
            _movingaverages.Add(new MovingAverage(key, PriceActions));
        }

        Trend?.Calculate();

        _averageHeights = new decimal[PriceActions.Length];
        _averageBodyHeights = new decimal[PriceActions.Length];
        _averageVolumes = new long[PriceActions.Length];
        _prologueSentiment = new TrendSentiment[PriceActions.Length];

        for (int p = 0; p < PriceActions.Length; p++)
        {
            if (_prologueLength > 0 && p > _prologueLength)
            {
                var prologue = Candlesticks[(p - _prologueLength - 1)..(p - 1)];
                _prologueSentiment[p] = prologue.All(pr => pr.High < Candlesticks[p].High)
                    ? TrendSentiment.Bullish
                    : prologue.All(pr => pr.Low > Candlesticks[p].Low)
                    ? TrendSentiment.Bearish
                    : TrendSentiment.Neutral;
            }
            else
            {
                _prologueSentiment[p] = TrendSentiment.None;
            }

            if (p == 0)
            {
                _averageHeights[p] = Candlesticks[p].Length;
                _averageBodyHeights[p] = Candlesticks[p].Body.Length;
                _averageVolumes[p] = Candlesticks[p].Volume;
            }
            else
            {
                _averageHeights[p] = _averageHeights[p - 1] + ((Candlesticks[p].Length - _averageHeights[p - 1]) / (p + 1));
                _averageBodyHeights[p] = _averageBodyHeights[p - 1] + ((Candlesticks[p].Body.Length - _averageBodyHeights[p - 1]) / (p + 1));
                _averageVolumes[p] = Convert.ToInt64(Math.Ceiling((decimal)_averageVolumes[p - 1] + ((Candlesticks[p].Volume - _averageVolumes[p - 1]) / (p + 1))));
            }
        }

        return this;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Chart);
    }

    public bool Equals(Chart? other)
    {
        return other is not null &&
               _prologueLength == other._prologueLength &&
               Interval == other.Interval &&
               Source == other.Source &&
               Code == other.Code &&
               Industry == other.Industry &&
               Sector == other.Sector &&
               Length == other.Length &&
               Start.Equals(other.Start) &&
               End.Equals(other.End);
    }

    public static int GetCacheKey(string? source, string? code, string? industry, string? sector,
        string? trend = null, int prologueLength = 15, ChartInterval interval = ChartInterval.Daily)
    {
        return HashCode.Combine(source, code, industry, sector, trend, prologueLength, interval);
    }

    public override int GetHashCode()
    {
        return GetCacheKey(Source, Code, Industry, Sector, Trend?.Name, _prologueLength, Interval);
    }
}