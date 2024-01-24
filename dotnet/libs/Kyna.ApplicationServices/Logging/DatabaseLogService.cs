using Kyna.Infrastructure.Database;

namespace Kyna.ApplicationServices.Logging;

public class DatabaseLogService(DbDef dbDef)
{
    private readonly IDbContext _dbContext = DbContextFactory.Create(dbDef.Engine, dbDef.ConnectionString);
    private readonly SqlRepository _sqlRepository = new(dbDef);

    public Task RemoveLogsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= new DateTime(1900, 1, 1);
        end ??= DateTime.UtcNow.AddDays(1);

        string sql = $"{_sqlRepository.DeleteLogs} WHERE timestamp_utc > @Start AND timestamp_utc < @End";

        return _dbContext.ExecuteAsync(sql, new { start, end });
    }

    public Task RemoveAppEventsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= new DateTime(1900, 1, 1);
        end ??= DateTime.UtcNow.AddDays(1);

        string sql = $"{_sqlRepository.DeleteAppEvents} WHERE timestamp_utc > @Start AND timestamp_utc < @End";

        return _dbContext.ExecuteAsync(sql, new { start, end });
    }
}
