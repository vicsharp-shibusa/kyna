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
}
