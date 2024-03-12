using Microsoft.Extensions.Configuration;

namespace Kyna.ApplicationServices.Reports;

public class ReportOptions
{
    public Stats? Stats { get; set; }
}
public class Stats
{
    [ConfigurationKeyName("Minimum Signals")]
    public int? MinimumSignals { get; set; }
}

