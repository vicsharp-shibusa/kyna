using Kyna.Infrastructure.Database.DataAccessObjects.Reports;

namespace Kyna.ApplicationServices.Reports;

public sealed partial class ReportService
{
    public IEnumerable<Report> CreateBacktestingReports(Guid processId)
    {
        var summaryReport = CreateReport("Report Details", "Detail", "Value");
        summaryReport.AddRow("Process Id", processId);
        summaryReport.AddRow("Time Generated", DateTime.Now);

        yield return summaryReport;

        var counts = _backtestsCtx.Query<SignalCounts>(
            _backtestsCtx.Sql.Backtests.FetchBacktestSignalCounts,
            new { processId });

        var scReport = CreateReport("Signal Counts",
            "Signal Name", "Result Direction", "Count", "Percentage");

        var signalNames = counts.Select(c => c.SignalName).Distinct().ToArray();

        SignalNameCount[] snCounts = new SignalNameCount[signalNames.Length];

        for (int i = 0; i < signalNames.Length; i++)
        {
            snCounts[i] = new SignalNameCount
            {
                Name = signalNames[i],
                Count = counts.Where(c => c.SignalName.Equals(signalNames[i])).Sum(c => c.Count)
            };
        }

        foreach (var count in counts)
        {
            var totalForSignal = snCounts.FirstOrDefault(s => s.Name.Equals(count.SignalName)).Count;
            var p = totalForSignal == 0 ? 0D : count.Count / (double)totalForSignal;
            scReport.AddRow(count.SignalName, count.ResultDirection, count.Count, p);
        }

        yield return scReport;

        foreach (var signalName in signalNames)
        {
            var signalSummaryReport = CreateReport($"{signalName} Summary",
                "Name", "Category", "Sub Category", "Number Signals", "Success %", "Avg Duration");

            var summary = _backtestsCtx.Query<SignalSummaryDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalSummary,
                new { processId, signalName });

            foreach (var item in summary.Where(d => d.NumberSignals >= (_reportOptions.Stats?.MinimumSignals ?? 0)))
            {
                signalSummaryReport.AddRow(item.Name, item.Category, item.SubCategory,
                    item.NumberSignals, item.SuccessPercentage, item.SuccessDuration);
            }

            yield return signalSummaryReport;

            var signalDetailReport = CreateReport($"{signalName} Details",
                "Name","Code","Industry","Sector",
                "Entry Date", "Entry Price Point", "Entry Price",
                "Result Up Date", "Result Up Price Point", "Result Up Price",
                "Result Down Date", "Result Down Price Point", "Result Down Price",
                "Result Direction", "Trading Days", "Calendar Days");

            var details = _backtestsCtx.Query<SignalDetails>(_backtestsCtx.Sql.Backtests.FetchBacktestSignalDetails,
                new { processId, signalName });

            foreach (var item in details)
            {
                signalDetailReport.AddRow(item.Name, item.Code, item.Industry, item.Sector,
                    item.EntryDate, item.EntryPricePoint, item.EntryPrice,
                    item.ResultUpDate, item.ResultUpPricePoint, item.ResultUpPrice,
                    item.ResultDownDate, item.ResultDownPricePoint, item.ResultDownPrice,
                    item.ResultDirection, item.TradingDays, item.CalendarDays);
            }

            yield return signalDetailReport;
        }
    }

    struct SignalNameCount
    {
        public string Name;
        public long Count;
    }
}
