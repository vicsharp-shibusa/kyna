using Kyna.Common;
using Kyna.Infrastructure.Database.DataAccessObjects.Reports;
using System.Diagnostics;

namespace Kyna.ApplicationServices.Reports;

public sealed partial class ReportService
{
    public IEnumerable<Report> CreateBacktestingReports(Guid processId)
    {
        string sql = $@"{_backtestsCtx.Sql.Backtests.FetchBacktest}
WHERE process_id = @ProcessId";

        var backtests = _backtestsCtx.Query<Infrastructure.Database.DataAccessObjects.Backtest>(
            sql, new { processId }).ToArray();

        Debug.Assert(backtests != null);

        List<string> headers = new List<string>(backtests.Length + 1) { "Details" };
        headers.AddRange(backtests.Select(b => b.Id.First8()).OrderBy(b => b));

        var summaryReport = CreateReport("Report Details", headers.ToArray());

        var names = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Name).ToList();
        var types = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Type).ToList();
        var sources = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Source).ToList();
        var descriptions = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.Description).ToList();
        var timestamps = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")).ToList();
        var processIds = backtests.OrderBy(b => b.Id.ToString()).Select(b => b.ProcessId.ToString()).ToList();

        names.Insert(0, "Name");
        types.Insert(0, "Type");
        sources.Insert(0, "Source");
        descriptions.Insert(0, "Description");
        timestamps.Insert(0, "Backtest (local) time");
        processIds.Insert(0, "Process Id");

        summaryReport.AddRow(names.ToArray());
        summaryReport.AddRow(types.ToArray());
        summaryReport.AddRow(sources.ToArray());
        summaryReport.AddRow(descriptions.ToArray());
        summaryReport.AddRow(timestamps.ToArray());
        summaryReport.AddRow(processIds.ToArray());

        yield return summaryReport;

        var counts = _backtestsCtx.Query<SignalCounts>(
            _backtestsCtx.Sql.Backtests.FetchBacktestSignalCounts,
            new { processId });

        var scReport = CreateReport("Signal Counts", "Backtest Id",
            "Signal Name", "Result Direction", "Count", "Percentage");

        var backtestIds = counts.Select(c => c.BacktestId).Distinct().ToArray();
        var signalNames = counts.Select(c => c.SignalName).Distinct().ToArray();

        SignalNameCount[] snCounts = new SignalNameCount[signalNames.Length * backtestIds.Length];

        int i = 0;
        foreach (var backtestId in backtestIds)
        {
            foreach (var signalName in signalNames)
            {
                snCounts[i++] = new SignalNameCount
                {
                    BacktestId = backtestId,
                    Name = signalName,
                    Count = counts.Where(c => c.BacktestId.Equals(backtestId) &&
                        c.SignalName.Equals(signalName)).Sum(c => c.Count)
                };
            }
        }

        foreach (var count in counts)
        {
            var totalForSignal = snCounts.FirstOrDefault(s => s.BacktestId.Equals(count.BacktestId) &&
                s.Name.Equals(count.SignalName)).Count;
            var p = totalForSignal == 0 ? 0D : count.Count / (double)totalForSignal;
            scReport.AddRow(count.BacktestId.First8(), count.SignalName, count.ResultDirection, count.Count, p);
        }

        yield return scReport;

        foreach (var signalName in signalNames)
        {
            foreach (var backtestId in backtestIds)
            {
                var signalSummaryReport = CreateReport(
                    $"Summary {backtestId.First8()}",
                    "Backtest Id", "Name", "Category", "Sub Category",
                    "Number Signals", "Success %", "Avg Duration");

                var summary = _backtestsCtx.Query<SignalSummaryDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalSummary,
                    new { processId, signalName });

                foreach (var item in summary.Where(d => d.NumberSignals >= (_reportOptions.Stats?.MinimumSignals ?? 0)))
                {
                    signalSummaryReport.AddRow(item.BacktestId.First8(),
                        item.Name, item.Category, item.SubCategory,
                        item.NumberSignals, item.SuccessPercentage, item.SuccessDuration);
                }

                yield return signalSummaryReport;

                var signalDetailReport = CreateReport(
                    $"Details {backtestId.First8()}", "Backtest Id",
                    "Name", "Code", "Industry", "Sector",
                    "Entry Date", "Entry Price Point", "Entry Price",
                    "Result Up Date", "Result Up Price Point", "Result Up Price",
                    "Result Down Date", "Result Down Price Point", "Result Down Price",
                    "Result Direction", "Trading Days", "Calendar Days");

                var details = _backtestsCtx.Query<SignalDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalDetails,
                    new { processId, signalName });

                foreach (var item in details)
                {
                    signalDetailReport.AddRow(item.BacktestId.First8(), item.Name, item.Code, item.Industry, item.Sector,
                        item.EntryDate, item.EntryPricePoint, item.EntryPrice,
                        item.ResultUpDate, item.ResultUpPricePoint, item.ResultUpPrice,
                        item.ResultDownDate, item.ResultDownPricePoint, item.ResultDownPrice,
                        item.ResultDirection, item.TradingDays, item.CalendarDays);
                }

                yield return signalDetailReport;
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
        public string Name;
        public string Type;
        public string Source;
        public string Description;
        public DateTime CreatedUtc;
        public int ResultCount;

        public override readonly string ToString() =>
            $"{CreatedUtc:yyyy-MM-dd HH:mm} | {Source} | {Name} | {Type} | {ProcessId} | {Description} | {ResultCount:#,##0} results";
    }
}

struct SignalNameCount
{
    public Guid BacktestId;
    public string Name;
    public long Count;
}

