using Kyna.Analysis.Technical.Trends;

namespace Kyna.Analysis.Technical;

public class Chart
{
    private readonly List<MovingAverage> _movingaverages = new(3);
    private readonly HashSet<MovingAverageKey> _movingAverageKeys = new(3);

    internal Chart(string? name, string? industry, string? sector,
        ChartInterval interval = ChartInterval.Daily)
    {
        Code = name;
        Industry = industry;
        Sector = sector;
        Interval = interval;
    }

    public ChartInterval Interval { get; }
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
        if (tolerance < 1M)
        {
            tolerance = 1M;
        }

        var lookbackPosition = lookbackPeriod == 0 ? 0 : Math.Max(position - lookbackPeriod, 0);

        var avg = Candlesticks[lookbackPosition..(position - 1)].Select(c => c.Body.Length).Average();
        return Candlesticks[position].Body.Length > (tolerance * avg);
    }

    public bool IsShort(int position, int lookbackPeriod = 0, decimal tolerance = 1M)
    {
        if (position < 10)
        {
            return false;
        }
        if (tolerance > 1M)
        {
            tolerance = 1M;
        }

        var lookbackPosition = lookbackPeriod == 0 ? 0 : Math.Max(position - lookbackPeriod, 0);

        var avg = Candlesticks[lookbackPosition..(position - 1)].Select(c => c.Body.Length).Average();
        return Candlesticks[position].Body.Length < (tolerance * avg);
    }

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

        return this;
    }
}