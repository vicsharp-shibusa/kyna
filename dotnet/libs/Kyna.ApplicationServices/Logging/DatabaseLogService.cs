using Kyna.Infrastructure.Database;
using System.Data;

namespace Kyna.ApplicationServices.Logging;

public sealed class DatabaseLogService
{
    private readonly DbDef _dbDef;
    private readonly IDbConnection _dbContext;

    public DatabaseLogService(DbDef dbDef)
    {
        _dbDef = dbDef;
        _dbContext = dbDef.GetConnection();
    }

    public Task RemoveLogsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= new DateTime(1900, 1, 1);
        end ??= DateTime.UtcNow.AddDays(1);

        var sql = _dbDef.GetSql(SqlKeys.DeleteLogs, "created_at > @Start", "created_at < @End");

        return _dbContext.ExecuteAsync(sql, new { start, end });
    }

    public Task RemoveAppEventsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= new DateTime(1900, 1, 1);
        end ??= DateTime.UtcNow.AddDays(1);

        var sql = _dbDef.GetSql(SqlKeys.DeleteAppEvents, "created_at > @Start", "created_at < @End");

        return _dbContext.ExecuteAsync(sql, new { start, end });
    }
}
