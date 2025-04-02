using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using System.Data;

namespace Kyna.Infrastructure.Database;

public sealed class DbDef : ISqlRepository
{
    public const string DefaultParmPrefix = "@";
    public string Name;
    public DatabaseEngine Engine;
    public string ConnectionString;

    internal SqlFactory Sql;

    public DbDef(string name, DatabaseEngine engine, string connectionString)
    {
        Name = name;
        Engine = engine;
        ConnectionString = connectionString;
        Sql = new SqlFactory(engine);
    }

    public string ParameterPrefix => Engine switch
    {
        _ => DefaultParmPrefix
    };

    public IDbConnection GetConnection()
    {
        switch (Engine)
        {
            case DatabaseEngine.PostgreSql:
                return new NpgsqlConnection(ConnectionString);
            case DatabaseEngine.MsSqlServer:
                return new SqlConnection(ConnectionString);
            case DatabaseEngine.MySql:
            case DatabaseEngine.MariaDb:
                return new MySqlConnection(ConnectionString);
            case DatabaseEngine.Sqlite:
                return new SqliteConnection(ConnectionString);
            case DatabaseEngine.None:
            default:
                throw new ArgumentException("Invalid or unsupported database engine.");
        }
    }

    public string? GetFormattedSqlWithWhereClause(string key,
        LogicalOperator logOp = LogicalOperator.And, params string[] whereClauses) =>
        Sql?.GetFormattedSqlWithWhereClause(key, logOp, whereClauses);

    public string? GetSql(string sql) => Sql?.GetSql(sql);

    public string? GetSql(string key, bool formatSql = false) =>
        Sql?.GetSql(key, formatSql);

    public string? GetSql(string key, LogicalOperator logOp, params string[] whereClauseKeys) =>
        Sql?.GetSql(key, logOp, whereClauseKeys);

    public string? GetSql(string key, params string[] whereClauseKeys) =>
        Sql?.GetSql(key, whereClauseKeys);

    public bool TryGetSql(string key, out string? statement, bool formatSql = false)
    {
        statement = null;
        return Sql?.TryGetSql(key, out statement, formatSql) ?? false;
    }
}
