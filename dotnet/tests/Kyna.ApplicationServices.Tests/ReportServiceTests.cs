using Kyna.ApplicationServices.Reports;

namespace Kyna.ApplicationServices.Tests;

public class ReportServiceTests
{
    [Fact]
    public void FileCreation()
    {
        var report = ReportService.CreateReport("People", "Name", "Age");

        report.AddRow("Alex", 44);
        report.AddRow("Raven", 33);

        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.xlsx"));
        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }
        ReportService.CreateSpreadsheet(fileInfo.FullName, report);
    }
}
