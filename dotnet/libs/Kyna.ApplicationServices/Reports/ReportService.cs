using ClosedXML.Excel;
using Kyna.ApplicationServices.Analysis;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;

namespace Kyna.ApplicationServices.Reports;

public sealed partial class ReportService(DbDef backtestsDbDef, DbDef financialsDbDef,
    ReportOptions reportOptions)
{
    private readonly IDbContext _backtestsCtx = DbContextFactory.Create(backtestsDbDef);
    private readonly IDbContext _financialsCtx = DbContextFactory.Create(financialsDbDef);
    private readonly ReportOptions _reportOptions = reportOptions;
    private readonly FinancialsRepository _financialsRepository = new(financialsDbDef);

    public event EventHandler<CommunicationEventArgs>? Communicate;
    
    private void ReportService_Communicate(object? sender, CommunicationEventArgs e)
    {
        Communicate?.Invoke(sender, e);
    }

    public static Report CreateReport(string name, params string[] headers) => new(name, headers);

    public static void CreateCsv(string file, Report report, string delimiter = ",")
    {
        var fileInfo = new FileInfo(file);
        if (fileInfo.Exists)
        {
            throw new ArgumentException($"File '{fileInfo.FullName}' already exists.");
        }

        using var stream = File.Create(fileInfo.FullName);
        stream.WriteLine(string.Join(delimiter, report.Headers));
        foreach (var row in report.Rows)
        {
            if (row != null)
            {
                stream.WriteLine(string.Join(delimiter, row.Select(r => r?.ToString() ?? "")));
            }
        }
        stream.Flush();
    }

    public static void CreateSpreadsheet(string file, params Report[] reports)
    {
        if (reports.Length == 0)
        {
            throw new ArgumentException("No report provided.");
        }

        var names = reports.Select(r => r.Name).Distinct().ToArray();
        if (names.Length < reports.Length)
        {
            throw new ArgumentException("Each report provided must have a unique name.");
        }

        var fileInfo = new FileInfo(file);
        if (fileInfo.Exists)
        {
            throw new ArgumentException($"File '{fileInfo.FullName}' already exists.");
        }

        var workbook = new XLWorkbook();

        foreach (var report in reports)
        {
            var sheet = workbook.Worksheets.Add(report.Name);

            var row = 1;
            var col = 1;
            foreach (var header in report.Headers)
            {
                sheet.Cell(row, col++).Value = header;
            }
            sheet.Range(sheet.Cell(row, 1), sheet.Cell(row, col)).Style.Font.Bold = true;
            sheet.Range(sheet.Cell(row, 1), sheet.Cell(row, col)).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            foreach (var dataRow in report.Rows)
            {
                row++;
                col = 1;
                foreach (var item in dataRow)
                {
                    sheet.Cell(row, col++).Value = XLCellValue.FromObject(item);
                }
            }
            sheet.Columns().AdjustToContents();
        }

        workbook.SaveAs(fileInfo.FullName);
        workbook.Dispose();
    }
}

public class Report
{
    private readonly List<object?[]> _rows;

    public Report(string name, IEnumerable<string> headers, int capacity = 1_000)
    {
        Headers = headers.ToArray();
        Name = name;
        _rows = new List<object?[]>(capacity);
        if (Headers.Length == 0)
        {
            throw new ArgumentException("At least one header is required.");
        }
    }

    public string Name { get; }
    public string[] Headers { get; }
    public object?[][] Rows => [.. _rows];

    public void AddRow(params object?[] data)
    {
        if (data.Length != Headers.Length)
        {
            throw new ArgumentException($"Expecting a row with {Headers.Length} items.");
        }
        _rows.Add(data);
    }

    public void AddRowRange(IEnumerable<object?[]> data)
    {
        foreach (var d in data)
        {
            AddRow(d);
        }
    }
}
