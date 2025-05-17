using Kyna.Analysis.Technical.Charts;

namespace Kyna.Analysis.Technical.Trends;

public class KynaTrend : PriceTrendBase, ITrend
{
    public KynaTrend(Ohlc[] prices) : base(prices)
    {
    }

    public void Calculate()
    {
        MovingAverageKey[] maKeys = [
            new MovingAverageKey(21),
            new MovingAverageKey(50),
            new MovingAverageKey(200)
        ];

        double[] weights = [
            0.37D, // best indicator
            0.24D, // next best
            0.16D, // and so on ...
            0.11D,
            0.08D,
            0.04D
        ];

        List<WeightedTrend> trends =
        [
            new WeightedTrend(new ExtremeTrend(_prices), weights[0]),
            new WeightedTrend(new PriceToMovingAverageTrend(maKeys[0], _prices), weights[1]),
            new WeightedTrend(new PriceToMovingAverageTrend(maKeys[1], _prices), weights[2]),
            new WeightedTrend(new PriceToMovingAverageTrend(maKeys[2], _prices), weights[3]),
            new WeightedTrend(new MultipleMovingAverageTrend(_prices, maKeys), weights[4]),
            new WeightedTrend(new CandlestickTrend(_prices),weights[5])
        ];

        var trend = new CombinedWeightedTrend([.. trends]);
        trend.Calculate();
        TrendValues = trend.TrendValues;
    }
}