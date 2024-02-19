namespace Kyna.Analysis.Technical;

public sealed class Chart
{
    private readonly List<MovingAverage> _movingaverages = new(3);
    private readonly HashSet<MovingAverageKey> _movingAverageKeys = new(3);
    private bool _includeCandles = false;

    public Chart()
    {
        PriceActions = [];
        Candlesticks = [];
    }

    public Ohlc[] PriceActions { get; private set; }
    public Candlestick[] Candlesticks { get; private set; }
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
        PriceActions = priceActions.ToArray();
        _includeCandles = true;
        return this;
    }

    public Chart WithCandles()
    {
        _includeCandles = true;
        return this;
    }

    public Chart Build()
    {
        if (_includeCandles)
        {
            Candlesticks = PriceActions.Select(p => new Candlestick(p)).ToArray();
        }

        _movingaverages.Clear();
        foreach (var key in _movingAverageKeys)
        {
            _movingaverages.Add(new MovingAverage(key, PriceActions));
        }

        return this;
    }
}