using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.DataImport;
using System.Diagnostics;
using System.Text.Json;

namespace Kyna.ApplicationServices.DataManagement;

public static class ImporterFactory
{
    public const string DefaultSource = PolygonImporter.SourceName;

    public static IExternalDataImporter Create(DbDef dbDef,
        FileInfo? configFileInfo = null,
        string? apiKey = null,
        string? accessKey = null,
        Guid? processId = null,
        bool dryRun = false)
    {
        var source = SourceUtility.GetSource(configFileInfo);

        if (source.Equals(PolygonImporter.SourceName, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException($"{nameof(apiKey)} is required to instantiate an importer for {source}");

            if (string.IsNullOrWhiteSpace(accessKey))
                throw new ArgumentException($"{nameof(accessKey)} is required to instantiate an importer for {source}");

            if (configFileInfo == null)
                throw new Exception($"A configuration file is required when instantiating {nameof(PolygonImporter)}");

            var polygonImportConfig = JsonSerializer.Deserialize<PolygonImporter.ImportConfigfile>(
                File.ReadAllText(configFileInfo.FullName),
                JsonSerializerOptionsRepository.Custom);

            Debug.Assert(polygonImportConfig != null);

            return new PolygonImporter(dbDef,
                new PolygonImporter.DataImportConfiguration(PolygonImporter.SourceName,
                    apiKey, accessKey,
                    polygonImportConfig.ImportActions,
                    polygonImportConfig.ImportFilePrefixes ?? [],
                    polygonImportConfig.Options),
                processId, dryRun);
        }

        throw new ArgumentException($"No importer defined for source {source}");
    }
}