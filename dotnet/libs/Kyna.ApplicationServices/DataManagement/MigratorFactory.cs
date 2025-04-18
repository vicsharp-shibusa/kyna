﻿using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.DataMigration;
using System.Diagnostics;
using System.Text.Json;

namespace Kyna.ApplicationServices.DataManagement;

public static class MigratorFactory
{
    public const string DefaultSource = PolygonMigrator.SourceName;

    public static IImportsMigrator Create(DbDef sourceDbDef, DbDef targetDbDef,
        FileInfo configFileInfo,
        Guid? processId = null,
        bool dryRun = false)
    {
        var options = JsonSerializerOptionsRepository.Custom;

        var source = SourceUtility.GetSource(configFileInfo);

        if (source.Equals(YahooMigrator.SourceName, StringComparison.OrdinalIgnoreCase))
        {
            options.Converters.Add(new EnumDescriptionConverter<YahooMigrator.SourceDeletionMode>());

            var yahooMigratorConfig = JsonSerializer.Deserialize<YahooMigrator.MigrationConfiguration>(
                File.ReadAllText(configFileInfo.FullName), options);

            Debug.Assert(yahooMigratorConfig != null);

            return new YahooMigrator(sourceDbDef, targetDbDef, yahooMigratorConfig, processId, dryRun);
        }

        if (source.Equals(PolygonMigrator.SourceName, StringComparison.OrdinalIgnoreCase))
        {
            options.Converters.Add(new EnumDescriptionConverter<PolygonMigrator.MigrationSourceMode>());
            options.Converters.Add(new EnumDescriptionConverter<PolygonMigrator.SourceDeletionMode>());

            var polygonMigratorConfig = JsonSerializer.Deserialize<PolygonMigrator.MigrationConfiguration>(
                File.ReadAllText(configFileInfo.FullName), options);

            Debug.Assert(polygonMigratorConfig != null);

            return new PolygonMigrator(sourceDbDef, targetDbDef, polygonMigratorConfig, processId, dryRun);
        }

        throw new ArgumentException($"No migrator defined for source {source}");
    }
}
