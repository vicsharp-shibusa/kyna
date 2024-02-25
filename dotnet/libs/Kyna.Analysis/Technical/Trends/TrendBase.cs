namespace Kyna.Analysis.Technical.Trends;

public interface ITrend
{
    TrendValue[] TrendValues { get; }
    void Calculate();
}

public struct TrendValue(TrendSentiment sentiment, double value)
{
    public TrendSentiment Sentiment = sentiment;
    public double Value = value;
}

public abstract class TrendBase
{
    public TrendBase(int size)
    {
        if (size < 1)
        {
            throw new ArgumentException($"Argument '{nameof(size)}' ({size}) cannot be less than 1 in {nameof(TrendBase)}");
        }

        TrendValues = new TrendValue[size];
    }

    public TrendValue[] TrendValues { get; }
}