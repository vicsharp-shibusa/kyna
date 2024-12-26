using System.ComponentModel;

namespace Kyna.Backtests.AlphaModel;

public enum BacktestType
{
    [Description("Random Baseline")]
    RandomBaseline = 0,
    [Description("Candlestick Pattern")]
    CandlestickPattern,
}