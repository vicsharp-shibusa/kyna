//using Kyna.Analysis.Technical.Charts;

//namespace Kyna.Analysis.Technical.Patterns;

//public sealed partial class CandlestickPatternRepository
//{
//    private static int IsTallWhiteCandle(Chart chart,
//        int position,
//        int numberRequired,
//        double volumeFactor = 1D)
//    {
//        CheckSignalArgs(chart, position, numberRequired);

//        var first = chart.Candlesticks[position];
//        var second = chart.Candlesticks[position + 1];

//        return second.IsLight &&
//            second.IsTallBody &&
//            chart.IsTall(position + 1) &&
//            second.Volume > first.Volume * volumeFactor
//            ? position + 1
//            : -1;
//    }
//}
