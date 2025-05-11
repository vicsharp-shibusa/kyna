using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Trends;

public abstract class PriceTrendBase
{
    protected readonly Ohlc[] _prices = [];

    public PriceTrendBase(Ohlc[] prices)
    {
        _prices = prices ?? [];
        TrendValues = new double[_prices.Length];
    }

    public double[] TrendValues { get; protected set; }

    public virtual string Name => GetType().Name;
}