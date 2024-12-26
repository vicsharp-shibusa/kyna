namespace Kyna.Analysis.Technical.Charts;

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

    public CandlestickColor Color => IsLight ? CandlestickColor.Light
        : IsDark ? CandlestickColor.Dark
        : CandlestickColor.None;

    public PriceRange Body => new(high: Math.Max(Open, Close), low: Math.Min(Open, Close));

    public PriceRange UpperShadow => new(High, Body.High);

    public PriceRange LowerShadow => new(Body.Low, Low);

    public bool IsTallBody => Length != 0 && Body.Length / Length > .8M;

    protected decimal TotalShadowLength => UpperShadow.Length + LowerShadow.Length;
    protected decimal UpperShadowToTotalShadowRatio => TotalShadowLength == 0 ? 0 : UpperShadow.Length / TotalShadowLength;
    protected decimal LowerShadowToTotalShadowRatio => TotalShadowLength == 0 ? 0 : LowerShadow.Length / TotalShadowLength;

    protected bool IsDojiBody => Volume > 0L
        && !IsFourPriceDoji
        && Body.Length < Length * .05M;

    public bool IsFourPriceDoji =>
        Body.Length == 0 &&
        (MidPoint < 5M ? Length == 0 : Length < .02M) &&
        Volume > 0L;

    public bool IsDoji => IsDojiBody &&
        !IsLongLeggedDoji &&
        !IsDragonflyDoji &&
        !IsGravestoneDoji;

    public bool IsLongLeggedDoji => IsDojiBody &&
        TotalShadowLength / AveragePrice > .05M;

    public bool IsDragonflyDoji => IsDojiBody &&
        UpperShadowToTotalShadowRatio < .15M &&
        LowerShadowToTotalShadowRatio > .85M;

    public bool IsGravestoneDoji => IsDojiBody &&
        LowerShadowToTotalShadowRatio < .15M &&
        UpperShadowToTotalShadowRatio > .85M;

    protected bool HasShavenHead => UpperShadow.Length == 0;
    protected bool HasShavenBottom => LowerShadow.Length == 0;

    public bool IsBullishBelthold => !IsDojiBody
        && !IsMarubozu
        && High != 0
        && Low != 0
        && Volume != 0
        && HasShavenBottom
        && IsLight
        && Body.Length > Length / 1.5M;

    public bool IsBearishBelthold => !IsDojiBody
        && !IsMarubozu
        && High != 0
        && Low != 0
        && Volume != 0
        && HasShavenHead
        && IsDark
        && Body.Length > Length / 1.5M;

    protected bool IsMarubozu => !IsDojiBody && Body.Length == Length && Body.Length > 0;

    public bool IsBullishMarubozu => IsMarubozu && IsLight;

    public bool IsBearishMarubozu => IsMarubozu && IsDark;

    public bool IsUmbrella => Length > 0
        && !IsDojiBody
        && LowerShadow.Length >= 2 * Body.Length
        && UpperShadow.Length <= Length * .1M
        && Body.Low > MidPoint;

    public bool IsInvertedUmbrella => Length > 0
        && !IsDojiBody
        && UpperShadow.Length >= 2 * Body.Length
        && LowerShadow.Length <= Length * .1M
        && Body.High < MidPoint;

    public bool IsSpinningTop => UpperShadow.Length > Body.Length
        && LowerShadow.Length > Body.Length
        && !IsDojiBody
        && !IsUmbrella
        && !IsInvertedUmbrella;
}
