namespace Kyna.Analysis.Technical.Trends;

public class MovingAverageTrend(MovingAverageKey movingAverageKey, Ohlc[] prices)
    : TrendBase(prices?.Length ?? 0), ITrend
{
    private readonly MovingAverage _movingAverage = new(movingAverageKey,
            prices!.Select(p => p.GetPricePoint(movingAverageKey.PricePoint)).ToArray());
    private readonly Ohlc[] _prices = prices!;

    public string Name => _movingAverage.Key.ToString();

    public void Calculate()
    {
        PricePoint pricePoint = movingAverageKey.PricePoint;
        for (int i = 0; i < _prices.Length; i++)
        {
            if (i < _movingAverage.Key.Period)
            {
                TrendValues[i] = new(TrendSentiment.Neutral, 0D);
            }
            else if ((_prices[i].GetPricePoint(pricePoint) > _movingAverage.Values[i] &&
                _prices[i - 1].GetPricePoint(pricePoint) > _movingAverage.Values[i - 1]) ||
                _prices[i].Low > _movingAverage.Values[i])
            {
                TrendValues[i] = new(TrendSentiment.Bullish, 1D);
            }
            else if ((_prices[i].GetPricePoint(pricePoint) < _movingAverage.Values[i] &&
                _prices[i - 1].GetPricePoint(pricePoint) < _movingAverage.Values[i - 1]) ||
                _prices[i].High < _movingAverage.Values[i])
            {
                TrendValues[i] = new(TrendSentiment.Bearish, -1D);
            }
            else
            {
                TrendValues[i] = TrendValues[i - 1];
            }
        }
    }
}
