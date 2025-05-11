using Kyna.Analysis.Technical;
using Kyna.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.ApplicationServices.Research;

public class ResearchConfiguration
{
    public string? Name { get; init; }
    public string? Source { get; init; }
    public string? Description { get; init; }
    [JsonPropertyName("Entry Price Point")]
    public PricePoint EntryPricePoint { get; init; }
    [JsonPropertyName("Max Parallelization")]
    public int MaxParallelization { get; init; } = 1;
    [JsonPropertyName("Chart Configuration")]
    public ChartConfiguration? ChartConfiguration { get; init; }

    public static ResearchConfiguration Create(FileInfo? fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo, nameof(fileInfo));

        if (!fileInfo.Exists)
            throw new ArgumentException($"{fileInfo.FullName} does not exist.");

        var options = JsonSerializerOptionsRepository.Custom;
        options.Converters.Add(new EnumDescriptionConverter<PricePoint>());

        return JsonSerializer.Deserialize<ResearchConfiguration>(
            File.ReadAllText(fileInfo.FullName),
            options) ?? throw new ArgumentException($"Could not deserialize {fileInfo.FullName}");
    }
}
