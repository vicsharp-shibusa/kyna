using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Kyna.ApplicationServices.Logging;

public static class LoggerFactory
{
    public static ILogger<T>? Create<T>(DbDef dbDef, LogLevel minLogLevel = LogLevel.Trace,
    Func<string, LogLevel, bool>? filter = null) =>
        Create<T>(dbDef.Engine, dbDef.ConnectionString, minLogLevel, filter);

    public static ILogger<T>? Create<T>(
        DatabaseEngine databaseEngine,
        string connectionString,
        LogLevel minLogLevel = LogLevel.Trace,
        Func<string, LogLevel, bool>? filter = null)
    {
        return databaseEngine switch
        {
            DatabaseEngine.PostgreSql => CreatePostgreSqlLogger<T>(connectionString, minLogLevel, filter),
            DatabaseEngine.MsSqlServer => throw new Exception("A logger for MS SQL Server has not yet been implemented."),
            _ => null
        };
    }

    public static ILogger<T> CreatePostgreSqlLogger<T>(
        string connectionString,
        LogLevel minLogLevel = LogLevel.Trace,
        Func<string, LogLevel, bool>? filter = null)
    {
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new Infrastructure.Logging.PostgreSQL.LoggerProvider(connectionString, filter));
            builder.SetMinimumLevel(minLogLevel);
        });

        return loggerFactory.CreateLogger<T>();
    }
}
