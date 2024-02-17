namespace Kyna.Analysis.Technical;

public record class Candlestick : Ohlc
{
    public Candlestick(string symbol, DateTime start, DateTime end,
    decimal open, decimal high, decimal low, decimal close, long volume, double factor = 1D)
        : base(symbol, start, end, open, high, low, close, volume, factor)
    {
    }

    public Candlestick(string symbol, DateOnly date,
        decimal open, decimal high, decimal low, decimal close, long volume, double factor = 1D)
        : base(symbol, date, open, high, low, close, volume, factor)
    {
    }

    public Candlestick(Ohlc ohlc) : base(ohlc) { }

    public CandlestickColor Color => IsUp ? CandlestickColor.Light
        : IsDown ? CandlestickColor.Dark
        : CandlestickColor.None;

    public PriceRange Body => new(high: Math.Max(Open, Close), low: Math.Min(Open, Close));

    public PriceRange UpperShadow => new(High, Body.High);

    public PriceRange LowerShadow => new(Body.Low, Low);

    protected decimal UpperShadowToBodyLengthRatio => UpperShadow.Length / Math.Max(Body.Length, .0001M);

    protected decimal LowerShadowToBodyLengthRatio => LowerShadow.Length / Math.Max(Body.Length, .0001M);

    protected bool IsDojiBody => (Body.Length == 0
        || (Length > 0 && (Body.Length / Length <= .002M)))
        && Volume > 0L;

    public bool IsDoji => IsDojiBody && UpperShadowToBodyLengthRatio >= .33M
        && LowerShadowToBodyLengthRatio >= .33M;

    public bool IsLongLeggedDoji => IsDojiBody && UpperShadowToBodyLengthRatio >= .45M
        && LowerShadowToBodyLengthRatio >= .45M;

    public bool IsDragonflyDoji => IsDojiBody && UpperShadowToBodyLengthRatio <= .1M
        && LowerShadowToBodyLengthRatio >= .33M;

    public bool IsGravestoneDoji => IsDojiBody && UpperShadowToBodyLengthRatio >= .33M
        && LowerShadowToBodyLengthRatio <= .1M;

    public bool IsFourPriceDoji => IsDojiBody && UpperShadowToBodyLengthRatio <= .0005M
        && LowerShadowToBodyLengthRatio <= .0005M;

    protected bool HasShavenHead => UpperShadow.Length == 0;

    protected bool HasShavenBottom => LowerShadow.Length == 0;

    public bool IsBullishBelthold => !IsDojiBody
        && High != 0
        && Low != 0
        && Volume != 0
        && HasShavenBottom
        && IsUp;

    public bool IsBearishBelthold => !IsDojiBody
        && High != 0
        && Low != 0
        && Volume != 0
        && HasShavenHead
        && IsDown;

    protected bool IsMarubozu => !IsDojiBody && Body.Length == Length && Body.Length > 0;

    public bool IsBullishMarubozu => IsMarubozu && IsUp;

    public bool IsBearishMarubozu => IsMarubozu && IsDown;

    public bool IsUmbrella => Length > 0
        && !IsDojiBody
        && LowerShadow.Length >= (2 * Body.Length)
        && UpperShadow.Length <= (Length / 10M)
        && Body.Low > MidPoint
        && Volume > 0L;

    public bool IsInvertedUmbrella => Length > 0
        && !IsDojiBody
        && UpperShadow.Length >= (2 * Body.Length)
        && LowerShadow.Length <= (Length / 10M)
        && Body.High < MidPoint
        && Volume > 0L;

    public bool IsSpinningTop => UpperShadow.Length > Body.Length
        && LowerShadow.Length > Body.Length
        && !IsDojiBody
        && !IsUmbrella
        && !IsInvertedUmbrella
        && Volume > 0L;
}
