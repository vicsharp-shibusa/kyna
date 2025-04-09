using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.Database.DataAccessObjects.Reports;
using System.Diagnostics;

namespace Kyna.ApplicationServices.Reports;

public sealed partial class ReportService
{
    public IEnumerable<string> CreateBacktestingCsvReports(Guid processId, string outputDir)
    {
        //TODO: there's a #fail here - the where clause is db specific (see the '_').
        string sql = $@"{_backtestDbDef.Sql.GetFormattedSqlWithWhereClause(SqlKeys.FetchBacktest, whereClauses: ["process_id = @ProcessId"])}";

        var backtestDaos = _backtestConn.Query<Backtest>(sql, new { processId }).ToArray();

        Debug.Assert(backtestDaos != null);

        var counts = _backtestConn.Query<SignalCounts>(
            _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestSignalCounts),
            new { processId }).ToArray();

        var signalNames = counts.Select(c => c.SignalName).Distinct().ToArray();

        var scReport = CreateReport($"{processId.First()}-summary", "Id",
            "Signal Name", "Result Direction", "Count", "Percentage", "Description");

        List<SignalNameCount> snCounts = new(counts.Length);

        foreach (var backtestId in backtestDaos.Select(b => b.Id))
        {
            foreach (var signalName in signalNames)
            {
                snCounts.Add(new SignalNameCount
                {
                    BacktestNum = backtestId.First(),
                    Name = signalName,
                    Count = counts.Where(c => c.BacktestId.Equals(backtestId) &&
                        c.SignalName.Equals(signalName)).Sum(c => c.Count)
                });
            }
        }

        foreach (var count in counts)
        {
            var num = count.BacktestId.First();
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
                    $"{snAbbrev}-{btDao.Id.First()}-summary",
                    "Name", "Category", "Sub Category",
                    "Number Signals", "Success %", "Avg Duration");

                var summary = _backtestConn.Query<SignalSummaryDetails>(
                    _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestSignalSummary),
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

                var details = _backtestConn.Query<SignalDetails>(
                    _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestSignalDetails),
                    new { BacktestId = btDao.Id, processId, signalName });

                var signalDetailReport = CreateReport(
                    $"{snAbbrev}-{btDao.Id.First()}-details",
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
        string sql = $@"{_backtestDbDef.Sql.GetFormattedSqlWithWhereClause(SqlKeys.FetchBacktest,
            whereClauses: ["process_id = @ProcessId"])}";

        var backtests = _backtestConn.Query<Backtest>(sql, new { processId }).ToArray();

        Debug.Assert(backtests != null);

        Dictionary<Guid, (string Num, string FileName)> map = new(backtests.Length);

        int i = 0;
        foreach (var b in backtests.OrderBy(b => b.Id.ToString()))
        {
            var num = (++i).ToString().PadLeft(3, '0');
            map.Add(b.Id, (num, $"backtest_stats_{processId.First()}_{num}.xlsx"));
        }

        List<Report> reports = [];

        List<string> headers = new(backtests.Length + 1) { "Details" };
        headers.AddRange(backtests.Select(b => map[b.Id].Num).OrderBy(b => b));

        var summaryReport = CreateReport("Report Details", [.. headers]);

        var names = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Name).ToList();
        var types = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Type).ToList();
        var sources = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Source).ToList();
        var descriptions = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Description).ToList();
        var timestamps = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")).ToList();
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

        summaryReport.AddRow([.. names]);
        summaryReport.AddRow([.. types]);
        summaryReport.AddRow([.. sources]);
        summaryReport.AddRow([.. descriptions]);
        summaryReport.AddRow([.. timestamps]);
        summaryReport.AddRow([.. processIds]);
        summaryReport.AddRow([.. backtestIds]);
        summaryReport.AddRow([.. fileNames]);

        reports.Add(summaryReport);

        var counts = _backtestConn.Query<SignalCounts>(
            _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestSignalCounts),
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

        var fn = Path.Combine(outputDir, $"backtest_stats_{processId.First()}_summary.xlsx");

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

                var summary = _backtestConn.Query<SignalSummaryDetails>(
                    _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestSignalSummary),
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

                var details = _backtestConn.Query<SignalDetails>(
                    _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestSignalDetails),
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

                fn = Path.Combine(outputDir, $"backtest_stats_{processId.First()}_{map[backtestId].Num}.xlsx");
                CreateSpreadsheet(fn, [.. reports]);
                yield return fn;
                signalDetailReport = null;
            }
        }
    }

    public Task<IEnumerable<ProcessIdInfo>> GetBacktestProcessesAsync() =>
        _backtestConn.QueryAsync<ProcessIdInfo>(
            _backtestDbDef.Sql.GetSql(SqlKeys.FetchBacktestsProcessIdInfo));

    public async Task DeleteProcessesAsync(params Guid[] processIds)
    {
        foreach (var pid in processIds)
        {
            await _backtestConn.ExecuteAsync(
                _backtestDbDef.Sql.GetSql(SqlKeys.DeleteBacktestsForProcessId),
                new { ProcessId = pid });
        }
    }
}

struct SignalNameCount
{
    public string BacktestNum;
    public string Name;
    public long Count;
}