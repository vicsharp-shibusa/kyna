using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.DataImport;
using System.Diagnostics;
using System.Text.Json;

namespace Kyna.ApplicationServices.DataManagement;

public static class ImporterFactory
{
    public const string DefaultSource = EodHdImporter.SourceName;

    public static IExternalDataImporter Create(string source, DbDef dbDef,
        FileInfo? configFileInfo = null,
        string? apiKey = null, Guid? processId = null,
        bool dryRun = false)
    {
        if (source.Equals(YahooImporter.SourceName, StringComparison.OrdinalIgnoreCase))
        {
            if (configFileInfo == null)
            {
                throw new ArgumentNullException(nameof(configFileInfo));
            }

            var yahooImportConfig = JsonSerializer.Deserialize<YahooImporter.ImportConfigfile>(
                File.ReadAllText(configFileInfo.FullName),
                JsonOptionsRepository.DefaultSerializerOptions);

            Debug.Assert(yahooImportConfig != null);

            return new YahooImporter(dbDef, new YahooImporter.DataImportConfiguration(yahooImportConfig.Options), processId, dryRun);
        }

        if (source.Equals(EodHdImporter.SourceName, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException($"{nameof(apiKey)} is required to instantiate an importer for {source}");
            }
            if (configFileInfo == null)
            {
                return new EodHdImporter(dbDef, apiKey, processId);
            }
            var eodHdImportConfig = JsonSerializer.Deserialize<EodHdImporter.ImportConfigfile>(
                File.ReadAllText(configFileInfo.FullName),
                JsonOptionsRepository.DefaultSerializerOptions);

            Debug.Assert(eodHdImportConfig != null);

            return new EodHdImporter(dbDef,
                new EodHdImporter.DataImportConfiguration(EodHdImporter.SourceName,
                    apiKey,
                    eodHdImportConfig.ImportActions,
                    eodHdImportConfig.Exchanges,
                    eodHdImportConfig.SymbolTypes,
                    eodHdImportConfig.Options,
                    eodHdImportConfig.DateRanges),
                processId, dryRun);
        }

        throw new ArgumentException($"No importer defined for source {source}");
    }
}
