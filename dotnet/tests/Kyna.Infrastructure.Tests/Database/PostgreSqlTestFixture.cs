using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Configuration;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlTestFixture : IDisposable
{
    private const string DbName = "Imports";
    //public IDbConnection Context { get; private set; }
    public DbDef Imports { get; private set; }
    public DbDef Logs { get; private set; }
    public DbDef Financials { get; private set; }
    public DbDef Backtests { get; private set; }

    public PostgreSqlTestFixture()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();
        Imports = new DbDef(DbName, DatabaseEngine.PostgreSql, configuration.GetConnectionString(nameof(Imports))!);
        Logs = new DbDef(DbName, DatabaseEngine.PostgreSql, configuration.GetConnectionString(nameof(Logs))!);
        Financials = new DbDef(DbName, DatabaseEngine.PostgreSql, configuration.GetConnectionString(nameof(Financials))!);
        Backtests = new DbDef(DbName, DatabaseEngine.PostgreSql, configuration.GetConnectionString(nameof(Backtests))!);
        //Context = Imports.GetConnection();
        //Debug.Assert(Context != null);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}