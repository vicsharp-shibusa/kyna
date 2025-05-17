using Kyna.Analysis.Technical.Charts;
using Kyna.Common;
using System.Text.Json;

namespace Kyna.ApplicationServices.Research;

public record ResearchResult
{
    public ResearchResult(ChartInfo chartInfo, DateOnly date,
        StatType type, string key, StatMeta meta, decimal epiloguePriceDeviation)
    {
        ChartInfo = chartInfo;
        Date = date;
        Type = type;
        Key = key;
        Meta = meta;
        EpiloguePriceDeviation = epiloguePriceDeviation;
    }

    public ChartInfo ChartInfo { get; }
    public DateOnly Date { get; }
    public StatType Type { get; }
    public string Key { get; }
    public StatMeta Meta { get; }
    public decimal EpiloguePriceDeviation { get; }
}

public enum StatType
{
    Signal = 0,
    Scenario = 1
}

public record StatMeta
{
    private static readonly JsonSerializerOptions _serOptions = JsonSerializerOptionsRepository.Custom;
    private string? _string = null;

    public StatMeta(double trendValue, int epilogueLength)
    {
        TrendValue = trendValue;
        EpilogueLength = epilogueLength;
    }

    public double TrendValue { get; init; }
    public int EpilogueLength { get; init; }

    public override string ToString()
    {
        _string ??= JsonSerializer.Serialize(this, _serOptions);
        return _string;
    }
}