using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Trends;

public class KynaTrend : PriceTrendBase, ITrend
{
    private MovingAverage[] _movingAverages = [];

    public KynaTrend(Ohlc[] prices) : base(prices)
    {
        _movingAverages = new MovingAverage[KynaTrendConfiguration.MovingAverageKeys.Length];
    }

    public void Calculate()
    {
        throw new NotImplementedException();
    }
}

public static class KynaTrendConfiguration
{
    public static MovingAverageKey[] MovingAverageKeys => [
        new MovingAverageKey(21), new MovingAverageKey(50), new MovingAverageKey(200)
    ];
}