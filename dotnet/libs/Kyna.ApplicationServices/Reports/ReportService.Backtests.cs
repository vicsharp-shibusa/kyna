using Kyna.Common;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.Database.DataAccessObjects.Reports;
using Kyna.Infrastructure.DataImport;
using System.Diagnostics;

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

    public IEnumerable<string> CreateBacktestingCsvReports(Guid processId, string outputDir)
    {
        string sql = $@"{_backtestsCtx.Sql.Backtests.FetchBacktest}
WHERE process_id = @ProcessId";

        var backtestDaos = _backtestsCtx.Query<Backtest>(
            sql, new { processId }).ToArray();

        Debug.Assert(backtestDaos != null);

        var counts = _backtestsCtx.Query<SignalCounts>(
            _backtestsCtx.Sql.Backtests.FetchBacktestSignalCounts, new { processId }).ToArray();

        var signalNames = counts.Select(c => c.SignalName).Distinct().ToArray();

        var scReport = CreateReport($"{processId.First8()}-summary", "Id",
            "Signal Name", "Result Direction", "Count", "Percentage", "Description");

        List<SignalNameCount> snCounts = new(counts.Length);

        foreach (var backtestId in backtestDaos.Select(b => b.Id))
        {
            foreach (var signalName in signalNames)
            {
                snCounts.Add(new SignalNameCount
                {
                    BacktestNum = backtestId.First8(),
                    Name = signalName,
                    Count = counts.Where(c => c.BacktestId.Equals(backtestId) &&
                        c.SignalName.Equals(signalName)).Sum(c => c.Count)
                });
            }
        }

        foreach (var count in counts)
        {
            var num = count.BacktestId.First8();
            var totalForSignal = snCounts.FirstOrDefault(s => s.BacktestNum.Equals(num) &&
                s.Name.Equals(count.SignalName)).Count;
            var p = totalForSignal == 0 ? 0D : count.Count / (double)totalForSignal;
            var desc = backtestDaos.FirstOrDefault(b => b.Id.Equals(count.BacktestId))?.Description;
            scReport.AddRow(num, count.SignalName, count.ResultDirection, count.Count, p, desc);
        }

        var fn = Path.Combine(outputDir, $"{scReport.Name}.csv");
        CreateCsv(fn, scReport, "|");
        yield return fn;

        scReport = null;

        foreach (var signalName in signalNames)
        {
            var snAbbrev = signalName.Replace(' ', '-').ToLower();

            foreach (var btDao in backtestDaos)
            {
                var signalSummaryReport = CreateReport(
                    $"{snAbbrev}-{btDao.Id.First8()}-summary",
                    "Name", "Category", "Sub Category",
                    "Number Signals", "Success %", "Avg Duration");

                var summary = _backtestsCtx.Query<SignalSummaryDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalSummary,
                    new { BacktestId = btDao.Id, signalName });

                foreach (var item in summary.Where(d => d.NumberSignals >= (_reportOptions.Stats?.MinimumSignals ?? 0)))
                {
                    signalSummaryReport.AddRow(item.Name, item.Category, item.SubCategory,
                        item.NumberSignals, item.SuccessPercentage, item.SuccessDuration);
                }

                fn = Path.Combine(outputDir, $"{signalSummaryReport.Name}.csv");
                CreateCsv(fn, signalSummaryReport, "|");
                yield return fn;

                signalSummaryReport = null;

                var details = _backtestsCtx.Query<SignalDetails>(
                    _backtestsCtx.Sql.Backtests.FetchBacktestSignalDetails,
                    new { BacktestId = btDao.Id, processId, signalName });

                var signalDetailReport = CreateReport(
                    $"{snAbbrev}-{btDao.Id.First8()}-details",
                    "Name", "Code", "Industry", "Sector",
                    "Entry Date", "Entry Price Point", "Entry Price",
                    "Result Up Date", "Result Up Price Point", "Result Up Price",
                    "Result Down Date", "Result Down Price Point", "Result Down Price",
                    "Result Direction", "Trading Days", "Calendar Days");

                foreach (var item in details)
                {
                    signalDetailReport.AddRow(item.Name, item.Code, item.Industry, item.Sector,
                        item.EntryDate, item.EntryPricePoint, item.EntryPrice,
                        item.ResultUpDate, item.ResultUpPricePoint, item.ResultUpPrice,
                        item.ResultDownDate, item.ResultDownPricePoint, item.ResultDownPrice,
                        item.ResultDirection, item.TradingDays, item.CalendarDays);
                }

                fn = Path.Combine(outputDir, $"{signalDetailReport.Name}.csv");
                CreateCsv(fn, signalDetailReport, "|");
                yield return fn;
                signalDetailReport = null;
            }
        }
    }

    public IEnumerable<string> CreateBacktestingXlsxReports(Guid processId, string outputDir)
    {
        string sql = $@"{_backtestsCtx.Sql.Backtests.FetchBacktest}
WHERE process_id = @ProcessId";

        var backtests = _backtestsCtx.Query<Infrastructure.Database.DataAccessObjects.Backtest>(
            sql, new { processId }).ToArray();

        Debug.Assert(backtests != null);

        Dictionary<Guid, (string Num, string FileName)> map = new(backtests.Length);

        int i = 0;
        foreach (var b in backtests.OrderBy(b => b.Id.ToString()))
        {
            var num = (++i).ToString().PadLeft(3, '0');
            map.Add(b.Id, (num, $"backtest_stats_{processId.First8()}_{num}.xlsx"));
        }

        List<Report> reports = new(2);

        List<string> headers = new(backtests.Length + 1) { "Details" };
        headers.AddRange(backtests.Select(b => map[b.Id].Num).OrderBy(b => b));

        var summaryReport = CreateReport("Report Details", [.. headers]);

        var names = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Name).ToList();
        var types = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Type).ToList();
        var sources = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Source).ToList();
        var descriptions = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Description).ToList();
        var timestamps = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")).ToList();
        var processIds = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.ProcessId.ToString()).ToList();
        var backtestIds = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Id.ToString()).ToList();
        var fileNames = backtests.OrderBy(b => b.Id.ToString()).Select(b => map[b.Id].FileName).ToList();

        names.Insert(0, "Name");
        types.Insert(0, "Type");
        sources.Insert(0, "Source");
        descriptions.Insert(0, "Description");
        timestamps.Insert(0, "Backtest (local) time");
        processIds.Insert(0, "Process Id");
        backtestIds.Insert(0, "Backtest Id");
        fileNames.Insert(0, "File Names");

        summaryReport.AddRow(names.ToArray());
        summaryReport.AddRow(types.ToArray());
        summaryReport.AddRow(sources.ToArray());
        summaryReport.AddRow(descriptions.ToArray());
        summaryReport.AddRow(timestamps.ToArray());
        summaryReport.AddRow(processIds.ToArray());
        summaryReport.AddRow(backtestIds.ToArray());
        summaryReport.AddRow(fileNames.ToArray());

        reports.Add(summaryReport);

        var counts = _backtestsCtx.Query<SignalCounts>(
            _backtestsCtx.Sql.Backtests.FetchBacktestSignalCounts,
            new { processId });

        var scReport = CreateReport("Signal Counts", "Number",
            "Signal Name", "Result Direction", "Count", "Percentage", "Description");

        var backtestNums = counts.Select(c => map[c.BacktestId].Num).Distinct().ToArray();
        var signalNames = counts.Select(c => c.SignalName).Distinct().ToArray();

        SignalNameCount[] snCounts = new SignalNameCount[signalNames.Length * backtestNums.Length];

        i = 0;
        foreach (var backtestNum in backtestNums)
        {
            foreach (var signalName in signalNames)
            {
                snCounts[i++] = new SignalNameCount
                {
                    BacktestNum = backtestNum,
                    Name = signalName,
                    Count = counts.Where(c => map[c.BacktestId].Num.Equals(backtestNum) &&
                        c.SignalName.Equals(signalName)).Sum(c => c.Count)
                };
            }
        }

        foreach (var count in counts)
        {
            var num = map[count.BacktestId].Num;
            var totalForSignal = snCounts.FirstOrDefault(s => s.BacktestNum.Equals(num) &&
                s.Name.Equals(count.SignalName)).Count;
            var p = totalForSignal == 0 ? 0D : count.Count / (double)totalForSignal;
            var desc = backtests.FirstOrDefault(b => b.Id.Equals(count.BacktestId))?.Description;
            scReport.AddRow(num, count.SignalName, count.ResultDirection, count.Count, p, desc);
        }

        reports.Add(scReport);

        var fn = Path.Combine(outputDir, $"backtest_stats_{processId.First8()}_summary.xlsx");

        CreateSpreadsheet(fn, [.. reports]);

        yield return fn;

        summaryReport = null;
        scReport = null;

        reports.Clear();

        foreach (var signalName in signalNames)
        {
            foreach (var backtestId in backtests.Select(b => b.Id))
            {
                var signalSummaryReport = CreateReport(
                    $"Summary {map[backtestId].Num}",
                    "Name", "Category", "Sub Category",
                    "Number Signals", "Success %", "Avg Duration");

                var summary = _backtestsCtx.Query<SignalSummaryDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalSummary,
                    new { backtestId, signalName });

                foreach (var item in summary.Where(d => d.NumberSignals >= (_reportOptions.Stats?.MinimumSignals ?? 0)))
                {
                    signalSummaryReport.AddRow(item.Name, item.Category, item.SubCategory,
                        item.NumberSignals, item.SuccessPercentage, item.SuccessDuration);
                }

                reports.Add(signalSummaryReport);

                var signalDetailReport = CreateReport(
                    $"Details {map[backtestId].Num}",
                    "Name", "Code", "Industry", "Sector",
                    "Entry Date", "Entry Price Point", "Entry Price",
                    "Result Up Date", "Result Up Price Point", "Result Up Price",
                    "Result Down Date", "Result Down Price Point", "Result Down Price",
                    "Result Direction", "Trading Days", "Calendar Days");

                var details = _backtestsCtx.Query<SignalDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalDetails,
                    new { backtestId, processId, signalName });

                foreach (var item in details)
                {
                    signalDetailReport.AddRow(item.Name, item.Code, item.Industry, item.Sector,
                        item.EntryDate, item.EntryPricePoint, item.EntryPrice,
                        item.ResultUpDate, item.ResultUpPricePoint, item.ResultUpPrice,
                        item.ResultDownDate, item.ResultDownPricePoint, item.ResultDownPrice,
                        item.ResultDirection, item.TradingDays, item.CalendarDays);
                }

                reports.Add(signalDetailReport);

                fn = Path.Combine(outputDir, $"backtest_stats_{processId.First8()}_{map[backtestId].Num}.xlsx");
                CreateSpreadsheet(fn, [.. reports]);
                yield return fn;
                signalDetailReport = null;
            }
        }
    }

    public Task<IEnumerable<ProcessIdInfo>> GetBacktestProcessesAsync() =>
        _backtestsCtx.QueryAsync<ProcessIdInfo>(_backtestsCtx.Sql.Backtests.FetchProcessIdInfo);

    public async Task DeleteProcessesAsync(params Guid[] processIds)
    {
        foreach (var pid in processIds)
        {
            await _backtestsCtx.ExecuteAsync(_backtestsCtx.Sql.Backtests.DeleteForProcessId,
                new { ProcessId = pid });
        }
    }

    public struct ProcessIdInfo
    {
        public Guid ProcessId;
        public int BacktestCount;
        public DateTime MinDate;
        public DateTime MaxDate;

        public override readonly string ToString() =>
            $"{ProcessId} | {BacktestCount,4} | {MinDate:yyyy-MM-dd HH:mm} | {MaxDate:yyyy-MM-dd HH:mm}";
    }
}

struct SignalNameCount
{
    public string BacktestNum;
    public string Name;
    public long Count;
}