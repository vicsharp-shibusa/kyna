using Kyna.Analysis.Technical.Trends;

namespace Kyna.Analysis.Technical;

public class Chart(string code, string? industry, string? sector)
{
    private readonly List<MovingAverage> _movingaverages = new(3);
    private readonly HashSet<MovingAverageKey> _movingAverageKeys = new(3);
    private bool _includeCandles = false;

    public string Code { get; } = code;
    public string? Industry { get; } = industry;
    public string? Sector { get; } = sector;
    private ITrend? Trend { get; set; } = null;
    public TrendValue[] TrendValues => Trend?.TrendValues ??
        Enumerable.Repeat(new TrendValue(TrendSentiment.Unknown, 0D), PriceActions.Length).ToArray();
    public Ohlc[] PriceActions { get; private set; } = [];
    public Candlestick[] Candlesticks { get; private set; } = [];
    public int Length => PriceActions.Length;
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
        PriceActions = priceActions.ToArray();
        return this;
    }

    public Chart WithCandles(IEnumerable<Ohlc> priceActions)
    {
        _includeCandles = true;
        return WithPriceActions(priceActions);
    }

    public Chart WithCandles()
    {
        _includeCandles = true;
        return this;
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

        if (_includeCandles)
        {
            Candlesticks = PriceActions.Select(p => new Candlestick(p)).ToArray();
        }
        
        _movingaverages.Clear();
        foreach (var key in _movingAverageKeys)
        {
            _movingaverages.Add(new MovingAverage(key, PriceActions));
        }

        Trend?.Calculate();

        return this;
    }
}