namespace Kyna.Analysis.Technical.Trends;

public class MovingAverageTrend(MovingAverageKey movingAverageKey, Ohlc[] prices,
    PricePoint pricePoint = PricePoint.Close) : TrendBase(prices?.Length ?? 0), ITrend
{
    private readonly MovingAverage _movingAverage = new(movingAverageKey,
            prices!.Select(p => p.GetPricePoint(pricePoint)).ToArray());
    private readonly Ohlc[] _prices = prices!;
    private readonly PricePoint _pricePoint = pricePoint;

    public void Calculate()
    {
        for (int i = 0; i < _prices.Length; i++)
        {
            if (i < _movingAverage.Key.Period)
            {
                TrendValues[i] = new(TrendSentiment.Neutral, 0D);
            }
            else if ((_prices[i].GetPricePoint(_pricePoint) > _movingAverage.Values[i] &&
                _prices[i - 1].GetPricePoint(_pricePoint) > _movingAverage.Values[i - 1]) ||
                _prices[i].Low > _movingAverage.Values[i])
            {
                TrendValues[i] = new(TrendSentiment.Bullish, 1D);
            }
            else if ((_prices[i].GetPricePoint(_pricePoint) < _movingAverage.Values[i] &&
                _prices[i - 1].GetPricePoint(_pricePoint) < _movingAverage.Values[i - 1]) ||
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
