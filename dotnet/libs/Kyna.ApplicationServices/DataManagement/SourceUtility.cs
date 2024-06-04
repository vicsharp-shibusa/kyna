using Kyna.Common;
using Kyna.Infrastructure.DataImport;
using System.Text.Json;

namespace Kyna.ApplicationServices.DataManagement;

public static class SourceUtility
{
    public static string GetSource(FileInfo? fileInfo)
    {
        var defaultSource = YahooImporter.SourceName;
        if (fileInfo == null)
        {
            return defaultSource;
        }
        var config = JsonSerializer.Deserialize<SourceConfig>(
            File.ReadAllText(fileInfo.FullName), JsonOptionsRepository.DefaultSerializerOptions);
        return config.Source ?? defaultSource;
    }

#pragma warning disable CS0649
    struct SourceConfig
    {
        public string? Source;
    }
#pragma warning restore CS0649
}
