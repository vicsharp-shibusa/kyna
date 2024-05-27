using Kyna.Analysis.Technical;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;

namespace Kyna.ApplicationServices.Reports;

public sealed partial class ReportService
{
    public IEnumerable<string> CreateSplitsComparisonCsvReport(string outputDir)
    {
        string sourceSql = "SELECT DISTINCT source FROM public.splits";

        var sources = _financialsCtx.Query<string>(sourceSql).ToArray();

        if (sources.Length < 2)
        {
            throw new ArgumentException("At least two sources are required to construct the splits comparison report.");
        }

        Dictionary<string, Split[]> tickersBySourceDictionary = [];

        var sharedTickers = new List<string>(10_000);

        foreach (var source in sources)
        {
            var sql = $"{_financialsCtx.Sql.Splits.Fetch} WHERE source = @Source";
            var splits = _financialsCtx.Query<Split>(sql, new { source });
            if (source == EodHdImporter.SourceName)
            {
                tickersBySourceDictionary.Add(source, splits.Select(s => new Split(source, s.Code.Replace(".US", ""))
                {
                    After = s.After,
                    Before = s.Before,
                    CreatedTicksUtc = s.CreatedTicksUtc,
                    ProcessId = s.ProcessId,
                    SplitDate = s.SplitDate,
                    UpdatedTicksUtc = s.UpdatedTicksUtc
                }).ToArray());
            }
            else
            {
                tickersBySourceDictionary.Add(source, splits.ToArray());
            }
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
        var codesAndDates = _financialsCtx.Query<CodeAndDates>(
            _financialsCtx.Sql.AdjustedEodPrices.FetchCodesAndDates,
            commandTimeout: 0);

        Dictionary<string, string[]> codesBySourceDictionary = new();

        foreach (var source in codesAndDates.Select(c => c.Source).Distinct())
        {
            codesBySourceDictionary.Add(source, codesAndDates.Where(c => c.Source.Equals(source))
                .Select(c => c.CommonCode).Distinct().ToArray());
        }

        List<string> headers = new(5) { "Code", "Date" };
        headers.AddRange(codesAndDates.Select(c => c.Source).Distinct());

        var comparisonReport = CreateReport($"chart-comparison", headers.ToArray());

        List<string> commonCodes = new(4_000);
        foreach (var key in codesBySourceDictionary.Keys)
        {
            if (commonCodes.Count == 0)
            {
                commonCodes.AddRange(codesBySourceDictionary[key]);
            }
            else
            {
                commonCodes = commonCodes.Intersect(codesBySourceDictionary[key]).ToList();
            }
        }

        foreach (var commonCode in commonCodes)
        {
            bool skip = false;
            var start = codesAndDates.Where(c => c.CommonCode.Equals(commonCode))
                .Select(c => c.Start).Max();
            var finish = codesAndDates.Where(c => c.CommonCode.Equals(commonCode))
                .Select(c => c.Finish).Min();

            Chart[] charts = new Chart[codesBySourceDictionary.Keys.Count];
            int chartIndex = 0;
            foreach (var source in codesBySourceDictionary.Keys)
            {
                var code = source.Equals(EodHdImporter.SourceName, StringComparison.OrdinalIgnoreCase)
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
                        LengthOfPrologue = 0,
                        MovingAverages = null,
                        Trends = null
                    },
                    null, null, ohlc);
            }

            if (skip)
            {
                continue;
            }
            
            foreach (var date in charts[0].PriceActions.Select(p => p.Date))
            {
                decimal[] closes = new decimal[chartIndex];

                for (int c = 0; c < chartIndex; c++)
                {
                    closes[c] = charts[c].PriceActions.Where(p => p.Date.Equals(date))
                        .Select(p => p.Close).FirstOrDefault();
                }

                if ((closes.Average() - closes[0]) > .01M)
                {
                    object[] data = new object[chartIndex + 2];
                    data[0] = charts[0].Code!;
                    data[1] = date;
                    for (int i = 0; i < closes.Length; i++)
                    {
                        data[i + 2] = closes[i];
                    }
                    comparisonReport.AddRow(data);
                }
            }
        }

        var fn = Path.Combine(outputDir, $"{comparisonReport.Name}.csv");
        CreateCsv(fn, comparisonReport, "|");
        return [fn];
    }
}

public struct CodeAndDates
{
    public string Source;
    public string Code;
    public DateOnly Start;
    public DateOnly Finish;
    public string CommonCode => Source.Equals("eodhd.com", StringComparison.OrdinalIgnoreCase)
        ? Code.Replace(".US", "")
        : Code;
}
