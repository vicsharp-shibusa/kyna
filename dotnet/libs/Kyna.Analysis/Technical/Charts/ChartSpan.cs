namespace Kyna.Analysis.Technical.Charts;

public class ChartSpan
{
    public ChartSpan(Chart chart, int position, int length)
    {
        ArgumentNullException.ThrowIfNull(chart);
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
        if (position < 0 || position >= chart.Length)
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be within the chart's bounds.");
        if (position + length > chart.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Position + length cannot exceed chart length.");

        Offset = position;
        ChartInfo = chart.Info;
        PriceActions = chart.PriceActions[position..(position + length)];
        Candlesticks = chart.Candlesticks[position..(position + length)];
        TrendValues = chart.TrendValues[position..(position + length)];

        if ((PriceActions.Length != Candlesticks.Length) ||
            Candlesticks.Length != TrendValues.Length)
        {
            throw new ArgumentException($"Could not construct a {nameof(ChartSpan)} object with equally sized array lengths.");
        }
    }

    public ChartInfo ChartInfo { get; }
    public int Offset { get; }
    public Ohlc[] PriceActions { get; } = [];
    public Candlestick[] Candlesticks { get; } = [];
    public double[] TrendValues { get; }
    public int Length => PriceActions.Length;
}
