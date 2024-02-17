namespace Kyna.Analysis.Technical;

public record class Chart
{
    public Chart(IEnumerable<Ohlc> ohlcs)
    {
        PriceActions = ohlcs.OrderBy(o => o.Date).ToArray();
    }

    public Ohlc[] PriceActions { get; }
    public int Length => PriceActions.Length;
    public DateOnly Start => PriceActions[0].Date;
    public DateOnly End => PriceActions[PriceActions.Length - 1].Date;
}

public record class CandlestickChart : Chart
{
    public CandlestickChart(IEnumerable<Ohlc> ohlcs) : base(ohlcs)
    {
        Candlesticks = PriceActions.Select(p => new Candlestick(p)).ToArray();
    }
    
    public Candlestick[] Candlesticks { get; }
}