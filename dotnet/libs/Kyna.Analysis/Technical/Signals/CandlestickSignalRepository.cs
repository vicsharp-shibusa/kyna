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
            new CandlestickSignal(SignalName.TallWhiteCandle,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.None,
                options: _signalOptions)
                {
                    IsMatch = IsTallWhiteCandle
                },
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
                },
            new CandlestickSignal(SignalName.BullishHammer,
                numberRequired: 1,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsBullishHammer
                },
            new CandlestickSignal(SignalName.BearishHammer,
                numberRequired: 1,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsBearishHammer
                },
            new CandlestickSignal(SignalName.DarkCloudCover,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsDarkCloudCover
                },
            new CandlestickSignal(SignalName.PiercingPattern,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsPiercing
                },
            new CandlestickSignal(SignalName.MorningStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsMorningStar
                }
            ,
            new CandlestickSignal(SignalName.EveningStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsEveningStar
                },
            new CandlestickSignal(SignalName.MorningDojiStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsMorningDojiStar
                },
            new CandlestickSignal(SignalName.EveningDojiStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsEveningDojiStar
                },
            new CandlestickSignal(SignalName.ShootingStar,
                numberRequired: 1,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsShootingStar
                },
            new CandlestickSignal(SignalName.InvertedHammer,
                numberRequired: 1,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsInvertedHammer
                },
            new CandlestickSignal(SignalName.BullishHarami,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsBullishHarami
                },
            new CandlestickSignal(SignalName.BearishHarami,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsBearishHarami
                },
            new CandlestickSignal(SignalName.BullishHaramiCross,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsBullishHaramiCross
                },
            new CandlestickSignal(SignalName.BearishHaramiCross,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsBearishHaramiCross
                },
            new CandlestickSignal(SignalName.TweezerTop,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsTweezerTop
                },
            new CandlestickSignal(SignalName.TweezerBottom,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsTweezerBottom
                },
            new CandlestickSignal(SignalName.BullishBelthold,
                numberRequired: 1,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsBullishBelthold
                },
            new CandlestickSignal(SignalName.BearishBelthold,
                numberRequired: 1,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsBearishBelthold
                },
            new CandlestickSignal(SignalName.UpsideGapTwoCrows,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsUpsideGapTwoCrows
                },
            new CandlestickSignal(SignalName.ThreeBlackCrows,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsThreeBlackCrows
                },
            new CandlestickSignal(SignalName.ThreeWhiteSoliders,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsThreeWhiteSoldiers
                },
            new CandlestickSignal(SignalName.BullishCounterattack,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish,
                options: _signalOptions)
                {
                    IsMatch = IsBullishCounterattack
                },
            new CandlestickSignal(SignalName.BearishCounterattack,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish,
                options: _signalOptions)
                {
                    IsMatch = IsBearishCounterattack
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
