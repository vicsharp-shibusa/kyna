using Kyna.Common;

namespace Kyna.Analysis.Technical.Signals;

public sealed partial class CandlestickSignalRepository
{
    private readonly SignalOptions _signalOptions;
    private readonly CandlestickSignal[] _signals;

    public CandlestickSignalRepository(SignalOptions signalOptions)
    {
        _signalOptions = signalOptions;
        _signals = [
            new CandlestickSignal(SignalName.BullishEngulfing,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
            {
                IsMatch = IsBullishEngulfing
            },
            new CandlestickSignal(SignalName.BearishEngulfing,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
            {
                IsMatch = IsBearishEngulfing
            }
        ];
    }

    public CandlestickSignal? Find(SignalName signalName) =>
        _signals.FirstOrDefault(s => s.SignalName == signalName);

    public CandlestickSignal? Find(string signalName) =>
        Find(signalName.GetEnumValueFromDescription<SignalName>());

    private static void CheckSignalArgs(Chart? chart,
        int position,
        int numberRequired,
        int lengthOfPrologue)
    {
        ArgumentNullException.ThrowIfNull(chart);
        if (chart.Candlesticks.Length == 0)
        {
            throw new ArgumentException("Chart must contain candlesticks.");
        }
        if (position < 0 || position >= chart.Length)
        {
            throw new IndexOutOfRangeException(nameof(position));
        }
        if (position < lengthOfPrologue)
        {
            throw new ArgumentException($"{nameof(position)} must be greater than length of prologue.");
        }
        if (position > chart.Length - numberRequired)
        {
            throw new ArgumentException($"{nameof(position)} must be less than chart length minus number of OHLC required.");
        }
        if (chart.TrendValues.Length == 0)
        {
            throw new ArgumentException("Chart must contain a trend.");
        }
    }
}
