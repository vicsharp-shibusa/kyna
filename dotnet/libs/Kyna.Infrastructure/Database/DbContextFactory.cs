namespace Kyna.Infrastructure.Database;

internal static class DbContextFactory
{
    public static IDbContext Create(DbDef dbDef)
    {
        if (dbDef.Engine == DatabaseEngine.PostgreSql)
        {
            return new PostgreSqlContext(dbDef);
        }

        throw new Exception($"The database engine {dbDef.Engine} is not currently supported.");
    }
}
