//using Kyna.Analysis.Technical.Charts;
//using Kyna.Analysis.Technical.Trends;
//using Kyna.Common;

//namespace Kyna.Analysis.Technical.Patterns;

//public abstract class OhlcPatternBase(
//    PatternName patternName,
//    int numberRequired,
//    TrendSentiment sentiment,
//    TrendSentiment requiredSentiment)
//{
//    public PatternName PatternName { get; init; } = patternName;

//    public string Name => PatternName.GetEnumDescription();

//    /// <summary>
//    /// Represents the number of OHLC or candles required to form the pattern.
//    /// </summary>
//    public int NumberRequired { get; init; } = numberRequired;

//    /// <summary>
//    /// Represents the expected sentiment of the pattern, or
//    /// which way the security should go after the occurrence of the pattern.
//    /// </summary>
//    public TrendSentiment Sentiment { get; init; } = sentiment;

//    /// <summary>
//    /// Represents the sentiment the security should be in
//    /// for this pattern to be effective.
//    /// </summary>
//    public TrendSentiment RequiredSentiment { get; init; } = requiredSentiment;

//    /// <summary>
//    /// Represents a function to determine if a given position on a chart
//    /// is a match for a specified pattern.
//    /// The arguments are Chart, position in chart, number of OHLC required, and volume factor.
//    /// Volume factor is the factor applied to volume on the key candle, when appropriate.
//    /// </summary>
//    public abstract Func<Chart, int, int, double, int> IsMatch { get; init; }

//    public virtual IEnumerable<PatternMatch> DiscoverMatches(Chart chart, 
//        Chart? market = null, 
//        int lookbackPeriod = 15,
//        bool signalOnlyWithMarket = false,
//        double volumeFactor = 1D)
//    {
//        lookbackPeriod = Math.Max(1, Math.Min(lookbackPeriod, 1_000));
//        if (chart.Length >= (NumberRequired + lookbackPeriod))
//        {
//            bool useMarket = signalOnlyWithMarket && market != null && market.PriceActions.Length > 0;

//            for (int i = lookbackPeriod; i < chart.Length - NumberRequired; i++)
//            {
//                if (useMarket)
//                {
//                    var index = market!.GetIndexOfDate(chart.PriceActions[i].Date);
//                    if (index > -1 &&
//                        (market.TrendValues[index].AsSentiment().IsBullish() ||
//                            market.TrendValues[index].AsSentiment().IsBearish()) &&
//                        market.TrendValues[index].AsSentiment() != Sentiment)
//                    {
//                        continue;
//                    }
//                }
//                int position = IsMatch(chart, i, NumberRequired, volumeFactor);
//                if (position > -1)
//                {
//                    yield return new PatternMatch()
//                    {
//                        SignalName = PatternName.GetEnumDescription(),
//                        Code = chart.Info.Code ?? "None",
//                        Industry = chart.Info.Industry,
//                        Sector = chart.Info.Sector,
//                        LookbackRange = new ChartPositionRange(i - lookbackPeriod, i - 1),
//                        PatternInfo = new ChartPositionRange(i, i + NumberRequired - 1),
//                        Position = position
//                    };
//                }
//            }
//        }
//    }
//}