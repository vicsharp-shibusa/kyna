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

    public bool IsDojiBody => Math.Abs(Open - Close) < Math.Max(GetTolerance(High - Low), 0.01M);

    public bool IsFourPriceDoji => IsDojiBody && Length < GetTolerance(AveragePrice, 0.001M);

    public bool IsDoji => IsDojiBody &&
        !IsLongLeggedDoji &&
        !IsDragonflyDoji &&
        !IsGravestoneDoji;

    public bool IsLongLeggedDoji => IsDojiBody && TotalShadowLength > GetTolerance(AveragePrice, 0.05M);

    public bool IsDragonflyDoji => IsDojiBody &&
        UpperShadowToTotalShadowRatio < GetTolerance(1M, 0.15M) &&
        LowerShadowToTotalShadowRatio > 1M - GetTolerance(1M, 0.15M);

    public bool IsGravestoneDoji => IsDojiBody &&
        LowerShadowToTotalShadowRatio < GetTolerance(1M, 0.15M) &&
        UpperShadowToTotalShadowRatio > 1M - GetTolerance(1M, 0.15M);

    public bool HasShavenHead => UpperShadow.Length < GetTolerance(AveragePrice, 0.001M);
    public bool HasShavenBottom => LowerShadow.Length < GetTolerance(AveragePrice, 0.001M);

    public bool IsBullishBelthold => !IsDojiBody
        && !IsMarubozu
        && High != 0
        && Low != 0
        && Volume != 0
        && HasShavenBottom
        && IsLight
        && Body.Length > (2M / 3M) * Length - GetTolerance(Length, 0.05M);

    public bool IsBearishBelthold => !IsDojiBody
        && !IsMarubozu
        && High != 0
        && Low != 0
        && Volume != 0
        && HasShavenHead
        && IsDark
        && Body.Length > (2M / 3M) * Length - GetTolerance(Length, 0.05M);

    protected bool IsMarubozu => !IsDojiBody && Body.Length == Length && Body.Length > 0;

    public bool IsBullishMarubozu => IsMarubozu && IsLight;

    public bool IsBearishMarubozu => IsMarubozu && IsDark;

    public bool IsUmbrella => Length > 0
        && !IsDojiBody
        && LowerShadow.Length >= 2 * Body.Length - GetTolerance(Body.Length, 0.05M)
        && UpperShadow.Length <= GetTolerance(Length, 0.1M)
        && Body.Low > MidPoint - GetTolerance(MidPoint, 0.01M);

    public bool IsInvertedUmbrella => Length > 0
        && !IsDojiBody
        && UpperShadow.Length >= 2 * Body.Length - GetTolerance(Body.Length, 0.05M)
        && LowerShadow.Length <= GetTolerance(Length, 0.1M)
        && Body.High < MidPoint + GetTolerance(MidPoint, 0.01M);

    public bool IsSpinningTop => UpperShadow.Length > Body.Length
        && LowerShadow.Length > Body.Length
        && !IsDojiBody
        && !IsUmbrella
        && !IsInvertedUmbrella;

    private static decimal GetTolerance(decimal baseValue, decimal factor = 0.01M) => 
        factor * baseValue;
}
