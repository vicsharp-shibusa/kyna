using System.ComponentModel;

namespace Kyna.Backtests;

public enum BacktestType
{
    [Description("Random Baseline")]
    RandomBaseline = 0,
    [Description("Candlestick Pattern")]
    CandlestickPattern,
}