using Kyna.Common;
using Kyna.Infrastructure.DataMigration;
using System.Text.Json;

namespace Kyna.ApplicationServices.DataManagement;

public static class SourceUtility
{
    public static string GetSource(FileInfo? fileInfo)
    {
        var defaultSource = YahooMigrator.SourceName;
        if (fileInfo == null)
        {
            return defaultSource;
        }
        var config = JsonSerializer.Deserialize<SourceConfig>(
            File.ReadAllText(fileInfo.FullName), JsonOptionsRepository.DefaultSerializerOptions);
        return config.Source ?? defaultSource;
    }

    struct SourceConfig
    {
        public SourceConfig() { Source = null; }
        public string? Source;
    }
}
