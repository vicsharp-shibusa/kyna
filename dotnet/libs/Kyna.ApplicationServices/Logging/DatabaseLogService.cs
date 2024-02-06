using Kyna.Infrastructure.Database;

namespace Kyna.ApplicationServices.Logging;

public class DatabaseLogService(DbDef dbDef)
{
    private readonly IDbContext _dbContext = DbContextFactory.Create(dbDef);

    public Task RemoveLogsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= new DateTime(1900, 1, 1);
        end ??= DateTime.UtcNow.AddDays(1);

        string sql = $"{_dbContext.Sql.Logs.Delete} WHERE timestamp_utc > @Start AND timestamp_utc < @End";

        return _dbContext.ExecuteAsync(sql, new { start, end });
    }

    public Task RemoveAppEventsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= new DateTime(1900, 1, 1);
        end ??= DateTime.UtcNow.AddDays(1);

        string sql = $"{_dbContext.Sql.AppEvents.Delete} WHERE timestamp_utc > @Start AND timestamp_utc < @End";

        return _dbContext.ExecuteAsync(sql, new { start, end });
    }
}
