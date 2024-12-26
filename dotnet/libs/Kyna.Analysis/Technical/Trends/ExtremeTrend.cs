using Kyna.Analysis.Technical.Charts;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Kyna.Analysis.Tests")]
namespace Kyna.Analysis.Technical.Trends;

public class ExtremeTrend(Ohlc[] prices) : TrendBase(prices?.Length ?? 0), ITrend
{
    private readonly Ohlc[] _prices = prices!;

    public string Name => "Extremes";

    public void Calculate()
    {
        var extremes = GetHighs().Union(GetLows()).OrderBy(x => x.Position).ToArray();

        int e = 0;

        Extreme? previousExtreme = null;

        for (int i = 0; i < _prices.Length; i++)
        {
            if (i < extremes[e].Position)
            {
                if (previousExtreme == null)
                {
                    TrendValues[i] = new(TrendSentiment.None, 0D);
                }
                else
                {
                    TrendValues[i] = new(previousExtreme!.Value.Sentiment, previousExtreme.Value.TrendValue);
                }
            }
            else if (i == extremes[e].Position)
            {
                // It's possible that a single position could be both a high and a low.
                List<Extreme> ex = new(2);
                int j = e;
                while (extremes[j].Position == i && j < extremes.Length - 1)
                {
                    ex.Add(extremes[j++]);
                }
                if (ex.Count == 1)
                {
                    TrendValues[i] = new(extremes[e].Sentiment, extremes[e].TrendValue);
                }
                else
                {
                    if (previousExtreme == null)
                    {
                        TrendValues[i] = new(TrendSentiment.None, 0D);
                    }
                    else
                    {
                        if (previousExtreme!.Value.Sentiment == TrendSentiment.Bullish)
                        {
                            TrendValues[i] = new(TrendSentiment.Bearish, -1D);
                        }
                        else if (previousExtreme!.Value.Sentiment == TrendSentiment.Bearish)
                        {
                            TrendValues[i] = new(TrendSentiment.Bullish, 1D);
                        }
                        else
                        {
                            TrendValues[i] = new(extremes[e].Sentiment, extremes[e].TrendValue);
                        }
                    }
                }
                e += ex.Count;
                if (e < extremes.Length)
                {
                    previousExtreme = extremes[e - 1];
                }
            }
        }
    }

    public IEnumerable<Extreme> GetHighs()
    {
        Extreme previousExtreme = default;

        for (int i = 1; i < _prices.Length - 1; i++)
        {
            if (_prices[i].High > _prices[i - 1].High &&
                _prices[i].High > _prices[i + 1].High)
            {
                var extreme = new Extreme()
                {
                    Position = i,
                    Price = _prices[i].High,
                    PricePoint = PricePoint.High,
                    ExtremeType = ExtremeType.High,
                    Sentiment = _prices[i].High == previousExtreme.Price
                        ? previousExtreme.Sentiment
                        : _prices[i].High > previousExtreme.Price
                            ? TrendSentiment.Bullish
                            : TrendSentiment.Bearish
                };
                yield return extreme;
                previousExtreme = extreme;
            }
            else if (_prices[i].High > _prices[i - 1].High &&
                _prices[i].High == _prices[i + 1].High &&
                i < _prices.Length - 2)
            {
                var p = i + 1;
                while (_prices[i].High == _prices[p].High && p < _prices.Length - 2)
                {
                    p++;
                }
                i = p;
                if (_prices[i].High > _prices[i + 1].High)
                {
                    var extreme = new Extreme()
                    {
                        Position = i,
                        Price = _prices[i].High,
                        PricePoint = PricePoint.High,
                        ExtremeType = ExtremeType.High,
                        Sentiment = _prices[i].High == previousExtreme.Price
                            ? previousExtreme.Sentiment
                            : _prices[i].High > previousExtreme.Price
                                ? TrendSentiment.Bullish
                                : TrendSentiment.Bearish
                    };
                    yield return extreme;
                    previousExtreme = extreme;
                }
            }
        }
    }

    public IEnumerable<Extreme> GetLows()
    {
        Extreme previousExtreme = default;

        for (int i = 1; i < _prices.Length - 1; i++)
        {
            if (_prices[i].Low < _prices[i - 1].Low &&
                _prices[i].Low < _prices[i + 1].Low)
            {
                var extreme = new Extreme()
                {
                    Position = i,
                    Price = _prices[i].Low,
                    PricePoint = PricePoint.Low,
                    ExtremeType = ExtremeType.Low,
                    Sentiment = _prices[i].Low == previousExtreme.Price
                        ? previousExtreme.Sentiment
                        : _prices[i].Low > previousExtreme.Price
                            ? TrendSentiment.Bullish
                            : TrendSentiment.Bearish
                };
                yield return extreme;
                previousExtreme = extreme;
            }
            else if (_prices[i].Low > _prices[i - 1].Low &&
                _prices[i].Low == _prices[i + 1].Low &&
                i < _prices.Length - 2)
            {
                var p = i + 1;
                while (_prices[i].Low == _prices[p].Low && p < _prices.Length - 2)
                {
                    p++;
                }
                i = p;
                if (_prices[i].Low < _prices[i + 1].Low)
                {
                    var extreme = new Extreme()
                    {
                        Position = i,
                        Price = _prices[i].Low,
                        PricePoint = PricePoint.Low,
                        ExtremeType = ExtremeType.Low,
                        Sentiment = _prices[i].Low == previousExtreme.Price
                            ? previousExtreme.Sentiment
                            : _prices[i].Low > previousExtreme.Price
                                ? TrendSentiment.Bullish
                                : TrendSentiment.Bearish
                    };
                    yield return extreme;
                    previousExtreme = extreme;
                }
            }
        }
    }
}

public struct Extreme
{
    public int Position;
    public ExtremeType ExtremeType;
    public PricePoint PricePoint;
    public decimal Price;
    public TrendSentiment Sentiment;
    public readonly double TrendValue => Sentiment == TrendSentiment.Bullish
        ? 1D
        : Sentiment == TrendSentiment.Bearish
            ? -1D
            : 0D;
}
