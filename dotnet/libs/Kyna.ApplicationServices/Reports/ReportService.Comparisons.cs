﻿using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Charts;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;

namespace Kyna.ApplicationServices.Reports;

public sealed partial class ReportService
{
    public IEnumerable<string> CreateSplitsComparisonCsvReport(string outputDir)
    {
        // TODO: rogue SQL.
        string sourceSql = "SELECT DISTINCT source FROM public.splits";

        var sources = _financialsConn.Query<string>(sourceSql).ToArray();

        if (sources.Length < 2)
        {
            throw new ArgumentException("At least two sources are required to construct the splits comparison report.");
        }

        Dictionary<string, Split[]> tickersBySourceDictionary = [];

        var sharedTickers = new List<string>(10_000);

        foreach (var source in sources)
        {
            var sql = _financialDbDef.Sql.GetSql(SqlKeys.SelectSplits, "source = @Source");
            var splits = _financialsConn.Query<Split>(sql, new { source });
            tickersBySourceDictionary.Add(source, [.. splits]);
        }

        var splitReport = CreateReport($"split-comparison", "Code", "Date", "Source", "Before", "After", "Source", "Before", "After");

        for (int i = 0; i < tickersBySourceDictionary.Keys.Count - 1; i++)
        {
            foreach (var split in tickersBySourceDictionary[tickersBySourceDictionary.Keys.ElementAt(i)])
            {
                for (int j = 1; j < tickersBySourceDictionary.Keys.Count; j++)
                {
                    var match = tickersBySourceDictionary[tickersBySourceDictionary.Keys.ElementAt(j)].FirstOrDefault(t => t.Code.Equals(split.Code) &&
                        t.SplitDate.Equals(split.SplitDate));
                    if (match != null)
                    {
                        if (split.Before != match.Before || split.After != match.After)
                        {
                            splitReport.AddRow(split.Code, split.SplitDate, tickersBySourceDictionary.Keys.ElementAt(i), split.Before, split.After,
                                tickersBySourceDictionary.Keys.ElementAt(j), match.Before, match.After);
                        }
                    }
                }
            }
        }

        var fn = Path.Combine(outputDir, $"{splitReport.Name}.csv");
        CreateCsv(fn, splitReport, "|");
        yield return fn;
    }

    public async Task<IEnumerable<string>> CreateChartComparisonCsvReportAsync(string outputDir)
    {
        var codesAndDates = _financialsConn.Query<CodeAndDates>(
            _financialDbDef.Sql.GetSql(SqlKeys.SelectAdjustedCodesAndDates), commandTimeout: 0);

        Dictionary<string, string[]> codesBySourceDictionary = [];

        foreach (var source in codesAndDates.Select(c => c.Source).Distinct())
        {
            codesBySourceDictionary.Add(source, [.. codesAndDates.Where(c => c.Source.Equals(source))
                .Select(c => c.Code).Distinct()]);
        }

        List<string> headers = new(5) { "Code", "Date" };
        headers.AddRange(codesAndDates.Select(c => c.Source).Distinct());
        headers.Add("Source");

        var comparisonReport = CreateReport($"chart-comparison", [.. headers]);

        List<string> commonCodes = new(4_000);
        foreach (var key in codesBySourceDictionary.Keys)
        {
            if (commonCodes.Count == 0)
            {
                commonCodes.AddRange(codesBySourceDictionary[key]);
            }
            else
            {
                commonCodes = [.. commonCodes.Intersect(codesBySourceDictionary[key])];
            }
        }

        int totalReviewed = 0;
        int totalTrouble = 0;
        int totalSkipped = 0;

        foreach (var commonCode in commonCodes)
        {
            bool skip = false;
            var start = codesAndDates.Where(c => c.Code.Equals(commonCode))
                .Select(c => c.Start).Max();
            var finish = codesAndDates.Where(c => c.Code.Equals(commonCode))
                .Select(c => c.Finish).Min();

            Chart[] charts = new Chart[codesBySourceDictionary.Keys.Count];
            int chartIndex = 0;
            foreach (var source in codesBySourceDictionary.Keys)
            {
                // TODO: this is an inappropriate use of the PolygonImporter.SourceName - should be more dynamic.
                var code = source.Equals(PolygonImporter.SourceName, StringComparison.OrdinalIgnoreCase)
                    ? $"{commonCode}.US"
                    : commonCode;

                var ohlc = (await _financialsRepository.GetOhlcForSourceAndCodeAsync(source, code,
                    start, finish, true)).ToArray();

                if (ohlc.Length == 0)
                {
                    skip = true;
                    break;
                }
                charts[chartIndex++] = ChartFactory.Create(source, $"{commonCode}",
                    new ChartConfiguration()
                    {
                        Interval = ChartInterval.Daily.ToString(),
                        LookbackLength = 0,
                        MovingAverages = null,
                        Trends = null
                    },
                    null, null, ohlc);
            }

            if (skip)
            {
                totalSkipped++;
                //Communicate?.Invoke(this, new Common.Events.CommunicationEventArgs(
                //    $"Skipping {commonCode}", nameof(ReportService)));
                continue;
            }

            foreach (var date in charts[0].PriceActions.Select(p => p.Date))
            {
                totalReviewed++;
                decimal[] closes = new decimal[chartIndex];

                for (int c = 0; c < chartIndex; c++)
                {
                    closes[c] = charts[c].PriceActions.Where(p => p.Date.Equals(date))
                        .Select(p => p.Close).FirstOrDefault();
                }

                if (Math.Abs(closes.Average() - closes[0]) > .01M)
                {
                    totalTrouble++;
                    object[] data = new object[chartIndex + 3];
                    data[0] = charts[0].Info.Code!;
                    data[1] = date;
                    for (int i = 0; i < closes.Length; i++)
                    {
                        data[i + 2] = closes[i];
                    }
                    data[^1] = DetermineInconsistentSource(
                        [.. codesBySourceDictionary.Keys], closes);
                    comparisonReport.AddRow(data);
                }
            }
        }

        var fn = Path.Combine(outputDir, $"{comparisonReport.Name}.csv");
        CreateCsv(fn, comparisonReport, "|");

        Communicate?.Invoke(this, new Infrastructure.Events.CommunicationEventArgs(
            $"Total Reviewed : {totalReviewed:#,##0}", nameof(ReportService)));
        Communicate?.Invoke(this, new Infrastructure.Events.CommunicationEventArgs(
            $"Total Skipped  : {totalSkipped:#,##0}", nameof(ReportService)));
        Communicate?.Invoke(this, new Infrastructure.Events.CommunicationEventArgs(
            $"Total Trouble  : {totalTrouble:#,##0}", nameof(ReportService)));

        return [fn];
    }

    private static string DetermineInconsistentSource(string[] sources, decimal[] closes)
    {
        decimal[] roundedCloses = [.. closes.Select(c => Math.Round(c, 2, MidpointRounding.ToZero))];

        var distinctCloses = roundedCloses.Distinct().ToArray();

        if (distinctCloses.Length == roundedCloses.Length)
        {
            return "All";
        }

        foreach (var distinctClose in roundedCloses)
        {
            var count = roundedCloses.Count(c => Math.Abs(c - distinctClose) < .02M);
            if (count == 1)
            {
                var index = Array.IndexOf(roundedCloses, distinctClose);
                return sources[index];
            }
        }

        return "Varied";
    }
}

public struct CodeAndDates
{
    public string Source;
    public string Code;
    public DateOnly Start;
    public DateOnly Finish;
}
