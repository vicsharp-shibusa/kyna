namespace Kyna.Infrastructure.Database;

internal static class DbContextFactory
{
    public static IDbContext Create(DbDef dbDef) => Create(dbDef.Engine, dbDef.ConnectionString);

    public static IDbContext Create(DatabaseEngine engine, string connectionString)
    {
        if (engine == DatabaseEngine.PostgreSql)
        {
            return new PostgreSqlContext(connectionString);
        }

        throw new Exception($"The database engine {engine} is not currently supported.");
    }
}
