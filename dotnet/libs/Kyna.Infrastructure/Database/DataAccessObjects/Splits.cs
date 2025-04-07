using Kyna.Infrastructure.Logging;
using System.Diagnostics;

namespace Kyna.Infrastructure.Database.DataAccessObjects;

internal sealed record class Split : DaoBase
{
    public Split() : this(source: "", code: "") { }

    public Split(string source, string code, Guid? processId = null) : base(processId)
    {
        Source = source;
        Code = code;
    }

    public Split(string source, string code,
        DateOnly splitDate, double before, double after,
        Guid? processId = null)
        : base(processId)
    {
        Source = source;
        Code = code;
        SplitDate = splitDate;
        Before = before;
        After = after;
    }

    public Split(string source, string code, DateOnly splitDate,
        string splitText, Guid? processId = null)
        : base(processId)
    {
        Source = source;
        Code = code;
        SplitDate = splitDate;
        (Before, After) = SplitAdjustedPriceCalculator.ConvertFromText(splitText);
    }

    public string Source { get; init; }
    public string Code { get; init; }
    public DateOnly SplitDate { get; internal set; }
    public double Before { get; init; }
    public double After { get; init; }
    public double Factor => Before == 0 ? 1 : (After / Before);

    public Analysis.Split ToAnalysisSplit() => new()
    {
        Source = Source,
        Code = Code,
        SplitDate = SplitDate,
        Before = Before,
        After = After
    };
}

internal static class SplitAdjustedPriceCalculator
{
    public static (double Before, double After) ConvertFromText(string text)
    {
        char splitter = text.Contains('/') ? '/' : ':';
        var textSplit = text.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

        Debug.Assert(textSplit.Length == 2);

        if (double.TryParse(textSplit[0], out var after) &&
            double.TryParse(textSplit[1], out var before))
        {
            return (before, after);
        }

        KyLogger.LogWarning($"Could not properly parse '{text}'", nameof(SplitAdjustedPriceCalculator));

        return (1, 1);
    }

    public static IEnumerable<EodAdjustedPrice> Calculate(IEnumerable<EodPrice> prices, IEnumerable<Split> splits)
    {
        var orderedPrices = prices.OrderBy(s => s.DateEod).ToArray();

        var orderedSplits = splits.OrderBy(s => s.SplitDate)
            .Where(s => s.SplitDate > orderedPrices[0].DateEod &&
                s.SplitDate < orderedPrices[^1].DateEod).ToArray();

        if (orderedSplits.Length == 0)
        {
            for (int i = 0; i < orderedPrices.Length; i++)
            {
                yield return new EodAdjustedPrice(orderedPrices[i]);
            }
        }
        else
        {
            var factors = new (DateOnly Date, double Factor)[orderedSplits.Length];
            double previousFactor = 1D;

            // loop backwards through the splits and create tuples of date/factor.
            // Factor is always multipled by the previous factor - where "previous"
            // is the next split chronologically and where previous factor starts at 1.
            for (int i = orderedSplits.Length - 1; i >= 0; i--)
            {
                factors[i] = (orderedSplits[i].SplitDate, previousFactor * orderedSplits[i].Factor);
                previousFactor = factors[i].Factor;
            }

            // Move the factor date to the next valid eod price date.
            // A scenario was found in which there were large gaps in the prices and multiple
            // splits between those gaps. (It's a problem with the source data).
            // This is the reason for this 'j' variable; if the first match
            // is already present in our factors array, we simply replace that existing value with the new
            // split value.
            int j = 0;
            for (int i = 0; i < factors.Length; i++)
            {
                var firstMatch = orderedPrices.FirstOrDefault(p => p.DateEod >= orderedSplits[i].SplitDate);
                if (firstMatch != null)
                {
                    if (i > 0 && factors[i - 1].Date.Equals(firstMatch.DateEod))
                    {
                        j--;
                    }
                    factors[j++].Date = firstMatch?.DateEod ?? factors[i].Date;
                }
            }

            // Resize this array because it's possible that some values were removed along the way (see comment above).
            factors = factors[..j];

            int f = 0;
            for (int i = 0; i < orderedPrices.Length; i++)
            {
                if (factors[f].Date.Equals(orderedPrices[i].DateEod) && f < factors.Length - 1)
                {
                    yield return new EodAdjustedPrice(orderedPrices[i], factors[++f].Factor);
                }
                else if (f == factors.Length - 1 && orderedPrices[i].DateEod >= factors[f].Date)
                {
                    yield return new EodAdjustedPrice(orderedPrices[i]);
                }
                else if (orderedPrices[i].DateEod < factors[f].Date)
                {
                    yield return new EodAdjustedPrice(orderedPrices[i], factors[f].Factor);
                }
                else if (f < factors.Length - 1 && orderedPrices[i].DateEod > factors[f].Date)
                {
                    yield return new EodAdjustedPrice(orderedPrices[i], factors[f + 1].Factor);
                    f++;
                }
            }

            // Make sure we didn't skip any entries.
            Debug.Assert(f == factors.Length - 1);
        }
    }
}