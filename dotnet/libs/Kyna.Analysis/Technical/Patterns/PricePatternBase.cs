using Kyna.Analysis.Technical.Charts;
using Kyna.Analysis.Technical.Trends;

namespace Kyna.Analysis.Technical.Patterns;

public abstract class PricePatternBase
{
    public PatternInfo Info { get; init; }

    public abstract Func<Ohlc[], bool> IsMatch { get; init; }

    public virtual bool IsMatchAdvanced(ChartSpan chartSpan,
        double volumeFactor = 1D,
        TrendSentiment requiredSentiment = TrendSentiment.None)
    {
        ArgumentNullException.ThrowIfNull(chartSpan);
        if (volumeFactor == 0)
            throw new ArgumentOutOfRangeException(nameof(volumeFactor),
                $"{nameof(volumeFactor)} cannot be zero - default to 1.0; to avoid volume checks, use {nameof(IsMatch)}");

        if (IsMatch(chartSpan.Candlesticks))
        {
            if (Info.NumberRequired > 1)
            {
                var first = chartSpan.Length - Info.NumberRequired;
                var last = chartSpan.Length - 1;
                if (volumeFactor > 0)
                {
                    if (chartSpan.PriceActions[first].Volume * volumeFactor <= chartSpan.PriceActions[last].Volume)
                        if (requiredSentiment != TrendSentiment.None &&
                            requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                            return true;
                }
                else
                {
                    if (chartSpan.PriceActions[first].Volume * volumeFactor >= chartSpan.PriceActions[last].Volume)
                        if (requiredSentiment != TrendSentiment.None &&
                            requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                            return true;
                }
            }
            else // NumberRequired = 1
            {
                if (requiredSentiment != TrendSentiment.None && requiredSentiment.HasFlag(chartSpan.TrendValues[0].AsSentiment()))
                    return true;
            }
        }
        return false;
    }

    public virtual bool IsContainedWithin(Ohlc[] ohlc)
    {
        for (int i = 0; i < ohlc.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(ohlc[i..(i + Info.NumberRequired)]))
                return true;
        }
        return false;
    }

    public virtual bool IsContainedWithinAdvanced(ChartSpan chartSpan,
        double volumeFactor = 1d,
        TrendSentiment requiredSentiment = TrendSentiment.None)
    {
        ArgumentNullException.ThrowIfNull(chartSpan);
        if (volumeFactor == 0)
            throw new ArgumentOutOfRangeException(nameof(volumeFactor),
                $"{nameof(volumeFactor)} cannot be zero - default to 1.0; to avoid volume checks, use {nameof(IsMatch)}");

        for (int i = 0; i < chartSpan.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(chartSpan.PriceActions[i..(i + Info.NumberRequired)]))
            {
                if (Info.NumberRequired > 1)
                {
                    var first = chartSpan.Length - Info.NumberRequired;
                    var last = chartSpan.Length - 1;
                    if (volumeFactor > 0)
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor <= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                return true;
                    }
                    else
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor >= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                return true;
                    }
                }
                else // NumberRequired = 1
                {
                    if (requiredSentiment != TrendSentiment.None && requiredSentiment.HasFlag(chartSpan.TrendValues[0].AsSentiment()))
                        return true;
                }
            }
        }
        return false;
    }

    public virtual IEnumerable<(int Start, int Finish)> FindAll(Ohlc[] ohlc)
    {
        for (int i = 0; i < ohlc.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(ohlc[i..(i + Info.NumberRequired)]))
                yield return (i, i + Info.NumberRequired);
        }
    }

    public virtual IEnumerable<(int Start, int Finish)> FindAllAdvanced(ChartSpan chartSpan,
        double volumeFactor = 1d,
        TrendSentiment requiredSentiment = TrendSentiment.None)
    {
        for (int i = 0; i < chartSpan.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(chartSpan.PriceActions[i..(i + Info.NumberRequired)]))
            {
                if (Info.NumberRequired > 1)
                {
                    var first = chartSpan.Length - Info.NumberRequired;
                    var last = chartSpan.Length - 1;
                    if (volumeFactor > 0)
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor <= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                yield return (i, i + Info.NumberRequired);
                    }
                    else
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor >= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                yield return (i, i + Info.NumberRequired);
                    }
                }
                else // NumberRequired = 1
                {
                    if (requiredSentiment != TrendSentiment.None && requiredSentiment.HasFlag(chartSpan.TrendValues[0].AsSentiment()))
                        yield return (i, i + Info.NumberRequired);
                }
            }
        }
    }

    public virtual IEnumerable<PatternMatch> FindAll(ChartSpan chartSpan)
    {
        foreach (var match in FindAll(chartSpan.PriceActions))
        {
            yield return new PatternMatch()
            {
                ChartInfo = chartSpan.ChartInfo,
                PatternInfo = Info,
                Date = chartSpan.PriceActions[match.Finish].Date,
                Location = new ChartPositionRange(match.Start, match.Finish)
            };
        }
    }

    public virtual (int Start, int Finish) FindFirstMatch(Ohlc[] ohlc)
    {
        for (int i = 0; i < ohlc.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(ohlc[i..(i + Info.NumberRequired)]))
                return (i, i + Info.NumberRequired);
        }
        return (-1, -1);
    }

    public virtual (int Start, int Finish) FindFirstMatchAdvanced(ChartSpan chartSpan,
        double volumeFactor = 1d,
        TrendSentiment requiredSentiment = TrendSentiment.None)
    {
        for (int i = 0; i < chartSpan.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(chartSpan.PriceActions[i..(i + Info.NumberRequired)]))
            {
                if (Info.NumberRequired > 1)
                {
                    var first = chartSpan.Length - Info.NumberRequired;
                    var last = chartSpan.Length - 1;
                    if (volumeFactor > 0)
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor <= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                return (i, i + Info.NumberRequired);
                    }
                    else
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor >= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                return (i, i + Info.NumberRequired);
                    }
                }
                else // NumberRequired = 1
                {
                    if (requiredSentiment != TrendSentiment.None && requiredSentiment.HasFlag(chartSpan.TrendValues[0].AsSentiment()))
                        return (i, i + Info.NumberRequired);
                }
            }
        }
        return (-1, -1);
    }

    public virtual (int Start, int Finish) FindLastMatch(Ohlc[] ohlc)
    {
        var lastMatch = (-1, -1);
        for (int i = 0; i < ohlc.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(ohlc[i..(i + Info.NumberRequired)]))
                lastMatch = (i, i + Info.NumberRequired);
        }
        return lastMatch;
    }

    public virtual (int Start, int Finish) FindLastMatchAdvanced(ChartSpan chartSpan,
        double volumeFactor = 1D,
        TrendSentiment requiredSentiment = TrendSentiment.None)
    {
        var lastMatch = (-1, -1);
        for (int i = 0; i < chartSpan.Length - Info.NumberRequired + 1; i++)
        {
            if (IsMatch(chartSpan.PriceActions[i..(i + Info.NumberRequired)]))
            {
                if (Info.NumberRequired > 1)
                {
                    var first = chartSpan.Length - Info.NumberRequired;
                    var last = chartSpan.Length - 1;
                    if (volumeFactor > 0)
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor <= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                lastMatch = (i, i + Info.NumberRequired);
                    }
                    else
                    {
                        if (chartSpan.PriceActions[first].Volume * volumeFactor >= chartSpan.PriceActions[last].Volume)
                            if (requiredSentiment != TrendSentiment.None &&
                                requiredSentiment.HasFlag(chartSpan.TrendValues[first].AsSentiment()))
                                lastMatch = (i, i + Info.NumberRequired);
                    }
                }
                else // NumberRequired = 1
                {
                    if (requiredSentiment != TrendSentiment.None && requiredSentiment.HasFlag(chartSpan.TrendValues[0].AsSentiment()))
                        lastMatch = (i, i + Info.NumberRequired);
                }
            }
        }
        return lastMatch;
    }
}

public record AdvancedMatchInfo
{
    public double VolumeFactor { get; init; } = 1D;
    public TrendSentiment RequiredSentiment { get; init; } = TrendSentiment.None;
}