using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Kyna.ApplicationServices.Logging;

public static class LoggerFactory
{
    public static ILogger<T>? Create<T>(DbDef dbDef, LogLevel minLogLevel = LogLevel.Trace,
        Func<string, LogLevel, bool>? filter = null)
    {
        return dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => CreatePostgreSqlLogger<T>(dbDef, minLogLevel, filter),
            DatabaseEngine.MsSqlServer => throw new Exception("A logger for MS SQL Server has not yet been implemented."),
            _ => null
        };
    }

    private static ILogger<T> CreatePostgreSqlLogger<T>(
        DbDef dbDef,
        LogLevel minLogLevel = LogLevel.Trace,
        Func<string, LogLevel, bool>? filter = null)
    {
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new Infrastructure.Logging.PostgreSQL.LoggerProvider(dbDef, filter));
            builder.SetMinimumLevel(minLogLevel);
        });

        return loggerFactory.CreateLogger<T>();
    }
}
