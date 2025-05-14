using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Data;
using System.Text.Json;

namespace Kyna.ApplicationServices.Research;

public sealed class ResearchStatsService
{
    private readonly DbDef _backtestsDbDef;
    private readonly IDbConnection _backtestConn;

    public ResearchStatsService(DbDef backtestsDbDef)
    {
        _backtestsDbDef = backtestsDbDef;
        _backtestConn = backtestsDbDef.GetConnection();
    }

    public async Task<Guid> CreateStatsBuild(ResearchConfiguration config,
        Guid processId)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        if (string.IsNullOrWhiteSpace(config.Source))
        {
            throw new ArgumentException($"{nameof(config.Source)} is required for {nameof(CreateStatsBuild)}");
        }

        var sql = _backtestsDbDef.Sql.GetSql(SqlKeys.UpsertStatsBuild);

        string configContent = JsonSerializer.Serialize(config,
            JsonSerializerOptionsRepository.Custom);
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

    //public async Task SaveResearchStatAsync()
    //{
    //}
}
