namespace Kyna.Analysis.Technical.Signals;

public sealed partial class CandlestickSignalRepository
{
    private static bool IsTallWhiteCandle(Chart chart,
        int position,
        int numberRequired,
        int lengthOfPrologue,
        double volumeFactor = 1D)
    {
        CheckSignalArgs(chart, position, numberRequired, lengthOfPrologue);

        //var prologue = chart.Candlesticks[(position - lengthOfPrologue)..(position - 1)];
        var first = chart.Candlesticks[position];
        var second = chart.Candlesticks[position + 1];

        return second.IsLight &&
            second.IsTallBody &&
            chart.IsTall(position + 1) &&
            second.Volume > first.Volume * volumeFactor;
    }
}
