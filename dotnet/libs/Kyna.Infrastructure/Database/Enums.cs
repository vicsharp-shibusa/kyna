namespace Kyna.Infrastructure.Database;

public enum DatabaseEngine
{
    None = 0,
    PostgreSql,
    MsSqlServer,
    MySql,
    Sqlite,
    MariaDb
}

public enum LogicalOperator
{
    And = 0,
    Or = 1
}