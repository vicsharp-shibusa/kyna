using Kyna.Analysis.Technical;
using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Text.Json;

namespace Kyna.ApplicationServices.Research;

public sealed class ResearchStatsService
{
    private readonly DbDef _backtestsDbDef;

    public ResearchStatsService(DbDef backtestsDbDef)
    {
        _backtestsDbDef = backtestsDbDef;
    }

    public async Task<Guid> CreateStatsBuildAsync(ResearchConfiguration config,
        Guid processId)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        if (string.IsNullOrWhiteSpace(config.Source))
        {
            throw new ArgumentException($"{nameof(config.Source)} is required for {nameof(CreateStatsBuildAsync)}");
        }

        var sql = _backtestsDbDef.Sql.GetSql(SqlKeys.UpsertStatsBuild);

        var serOptions = JsonSerializerOptionsRepository.Custom;
        serOptions.Converters.Add(new EnumDescriptionConverter<PricePoint>());
        string configContent = JsonSerializer.Serialize(config, serOptions);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var dao = new StatsBuild(processId)
        {
            Id = Guid.NewGuid(),
            ConfigContent = configContent,
            CreatedAt = now,
            UpdatedAt = now,
            ProcessId = processId,
            Source = config.Source
        };

        using var conn = _backtestsDbDef.GetConnection();

        await conn.ExecuteAsync(sql, dao);

        conn.Close();

        return dao.Id;
    }

    public async Task SaveResearchStatAsync(Guid processId,
        Guid statBuildId,
        string code,
        DateOnly entryDate,
        string statType,
        string statKey,
        double statValue,
        string meta,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var stat = new StatsDetail(processId)
        {
            Code = code,
            EntryDate = entryDate,
            StatMeta = meta,
            StatsBuildId = statBuildId,
            StatType = statType,
            StatKey = statKey,
            StatVal = statValue,
            CreatedAt = now,
            UpdatedAt = now,
            ProcessId = processId
        };

        var sql = _backtestsDbDef.Sql.GetSql(SqlKeys.UpsertStatsDetail);
        using var conn = _backtestsDbDef.GetConnection();
        await conn.ExecuteAsync(sql, stat, cancellationToken: cancellationToken);
        conn.Close();
    }
}
