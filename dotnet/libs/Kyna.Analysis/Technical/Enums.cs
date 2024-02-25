namespace Kyna.Analysis.Technical;

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
    Unknown = 0,
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