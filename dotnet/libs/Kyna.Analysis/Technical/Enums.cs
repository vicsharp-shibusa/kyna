using System.ComponentModel;

namespace Kyna.Analysis.Technical;

public enum ChartInterval
{
    Daily = 0,
    Weekly,
    Monthly,
    Quarterly,
    Annually
}

public enum CandlestickColor
{
    None = 0,
    Light,
    Dark
}

public enum PricePoint
{
    MidPoint = 0,
    Open,
    High,
    Low,
    Close
}

public enum TrendSentiment
{
    None = 0,
    Bullish,
    Bearish,
    Neutral
}

public enum ExtremeType
{
    None = 0,
    High = 1,
    Low = 2
}

public enum SignalName
{
    None = 0,
    [Description("Tall White Candle")]
    TallWhiteCandle,
    [Description("Bullish Engulfing")]
    BullishEngulfing,
    [Description("Bullish Engulfing With FollowThru")]
    BullishEngulfingWithFollowThru,
    [Description("Bullish Engulfing With Four Black Predecessors")]
    BullishEngulfingWithFourBlackPredecessors,
    [Description("Bearish Engulfing")]
    BearishEngulfing,
    [Description("Bearish Engulfing With FollowThru")]
    BearishEngulfingWithFollowThru,
    [Description("Bullish Hammer")]
    BullishHammer,
    [Description("Bearish Hammer")]
    BearishHammer,
    [Description("Dark Cloud Cover")]
    DarkCloudCover,
    [Description("Piercing Pattern")]
    PiercingPattern,
    [Description("Morning Star")]
    MorningStar,
    [Description("Evening Star")]
    EveningStar,
    [Description("Morning Doji Star")]
    MorningDojiStar,
    [Description("Evening Doji Star")]
    EveningDojiStar,
    [Description("Shooting Star")]
    ShootingStar,
    [Description("Inverted Hammer")]
    InvertedHammer,
    [Description("Bullish Harami")]
    BullishHarami,
    [Description("Bearish Harami")]
    BearishHarami,
    [Description("Bullish Harami Cross")]
    BullishHaramiCross,
    [Description("Bearish Harami Cross")]
    BearishHaramiCross,
    [Description("Tweezer Top")]
    TweezerTop,
    [Description("Tweezer Bottom")]
    TweezerBottom,
    [Description("Bullish Belthold")]
    BullishBelthold,
    [Description("Bearish Belthold")]
    BearishBelthold,
    [Description("Upside Gap Two Crows")]
    UpsideGapTwoCrows,
    [Description("Three Black Crows")]
    ThreeBlackCrows,
    [Description("Three White Soliders")]
    ThreeWhiteSoliders,
    [Description("Bullish Counterattack")]
    BullishCounterattack,
    [Description("Bearish Counterattack")]
    BearishCounterattack
}