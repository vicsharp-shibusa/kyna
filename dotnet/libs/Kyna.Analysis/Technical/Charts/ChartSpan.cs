namespace Kyna.Analysis.Technical.Charts;

public class ChartSpan
{
    public ChartSpan(Chart chart, int start, int finish)
    {
        ArgumentNullException.ThrowIfNull(chart);
        if (start > finish)
            (start, finish) = (finish, start);

        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), $"{nameof(start)} cannot be negative.");
        if (finish > chart.Length)
            throw new ArgumentOutOfRangeException(nameof(finish), $"{nameof(finish)} cannot be greater than length of chart.");

        Offset = start;
        ChartInfo = chart.Info;
        PriceActions = chart.PriceActions[start..finish];
        Candlesticks = chart.Candlesticks[start..finish];
        TrendValues = chart.TrendValues[start..finish];

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
