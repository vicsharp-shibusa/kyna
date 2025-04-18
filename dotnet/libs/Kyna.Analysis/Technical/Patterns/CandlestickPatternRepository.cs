using Kyna.Analysis.Technical.Charts;
using Kyna.Common;

namespace Kyna.Analysis.Technical.Patterns;

public sealed partial class CandlestickPatternRepository
{
    private readonly CandlestickPattern[] _signals;

    public CandlestickPatternRepository()
    {
        _signals = [
            new CandlestickPattern(PatternName.TallWhiteCandle,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Neutral)
                {
                    IsMatch = IsTallWhiteCandle
                },
            new CandlestickPattern(PatternName.BullishEngulfing,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishEngulfing
                },
            new CandlestickPattern(PatternName.BullishEngulfingWithFollowThru,
                numberRequired: 9,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishEngulfingWithFollowThru
                },
            new CandlestickPattern(PatternName.BullishEngulfingWithFourBlackPredecessors,
                numberRequired: 6,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishEngulfingWithFourBlackPredecessors
                },
            new CandlestickPattern(PatternName.BullishEngulfingWithTallCandles,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishEngulfingWithTallCandles
                },
            new CandlestickPattern(PatternName.BearishEngulfing,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishEngulfing
                },
            new CandlestickPattern(PatternName.BearishEngulfingWithFollowThru,
                numberRequired: 9,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishEngulfingWithFollowThru
                },
            new CandlestickPattern(PatternName.BearishEngulfingWithTallCandles,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishEngulfingWithTallCandles
                },
            new CandlestickPattern(PatternName.BearishEngulfingWithFourWhitePredecessors,
                numberRequired: 6,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishEngulfingWithFourWhitePredecessors
                },
            new CandlestickPattern(PatternName.BullishHammer,
                numberRequired: 1,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishHammer
                },
            new CandlestickPattern(PatternName.BullishHammerWithFollowThru,
                numberRequired: 8,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishHammerWithFollowThru
                },
            new CandlestickPattern(PatternName.BearishHammer,
                numberRequired: 1,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishHammer
                },
            new CandlestickPattern(PatternName.BearishHammerWithFollowThru,
                numberRequired: 8,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishHammerWithFollowThru
                },
            new CandlestickPattern(PatternName.DarkCloudCover,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsDarkCloudCover
                },
            new CandlestickPattern(PatternName.DarkCloudCoverWithFollowThru,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsDarkCloudCoverWithFollowThru
                },
            new CandlestickPattern(PatternName.PiercingPattern,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsPiercing
                },
            new CandlestickPattern(PatternName.PiercingPatternWithFollowThru,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsPiercingWithFollowThru
                },
            new CandlestickPattern(PatternName.MorningStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsMorningStar
                }
            ,
            new CandlestickPattern(PatternName.EveningStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsEveningStar
                },
            new CandlestickPattern(PatternName.MorningDojiStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsMorningDojiStar
                },
            new CandlestickPattern(PatternName.EveningDojiStar,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsEveningDojiStar
                },
            new CandlestickPattern(PatternName.ShootingStar,
                numberRequired: 1,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsShootingStar
                },
            new CandlestickPattern(PatternName.InvertedHammer,
                numberRequired: 1,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsInvertedHammer
                },
            new CandlestickPattern(PatternName.BullishHarami,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishHarami
                },
            new CandlestickPattern(PatternName.BearishHarami,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishHarami
                },
            new CandlestickPattern(PatternName.BullishHaramiCross,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishHaramiCross
                },
            new CandlestickPattern(PatternName.BearishHaramiCross,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishHaramiCross
                },
            new CandlestickPattern(PatternName.TweezerTop,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsTweezerTop
                },
            new CandlestickPattern(PatternName.TweezerBottom,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsTweezerBottom
                },
            new CandlestickPattern(PatternName.BullishBelthold,
                numberRequired: 1,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishBelthold
                },
            new CandlestickPattern(PatternName.BearishBelthold,
                numberRequired: 1,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishBelthold
                },
            new CandlestickPattern(PatternName.UpsideGapTwoCrows,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsUpsideGapTwoCrows
                },
            new CandlestickPattern(PatternName.ThreeBlackCrows,
                numberRequired: 3,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsThreeBlackCrows
                },
            new CandlestickPattern(PatternName.ThreeWhiteSoliders,
                numberRequired: 3,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsThreeWhiteSoldiers
                },
            new CandlestickPattern(PatternName.BullishCounterattack,
                numberRequired: 2,
                sentiment: TrendSentiment.Bullish,
                requiredSentiment: TrendSentiment.Bearish)
                {
                    IsMatch = IsBullishCounterattack
                },
            new CandlestickPattern(PatternName.BearishCounterattack,
                numberRequired: 2,
                sentiment: TrendSentiment.Bearish,
                requiredSentiment: TrendSentiment.Bullish)
                {
                    IsMatch = IsBearishCounterattack
                }
        ];
    }

    public CandlestickPattern? Find(PatternName signalName) =>
        _signals.FirstOrDefault(s => s.PatternName == signalName);

    public CandlestickPattern? Find(string signalName) =>
        Find(signalName.GetEnumValueFromDescription<PatternName>());

    private static void CheckSignalArgs(Chart? chart,
        int position,
        int numberRequired)
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
        if (position > chart.Length - numberRequired)
        {
            throw new ArgumentException($"{nameof(position)} must be less than chart length minus the number of OHLC required.");
        }
        if (chart.TrendValues.Length == 0)
        {
            throw new ArgumentException("Chart must contain a trend.");
        }
    }
}
