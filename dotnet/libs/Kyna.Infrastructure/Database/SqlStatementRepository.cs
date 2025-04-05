using System.Text;

namespace Kyna.Infrastructure.Database;

internal static partial class SqlStatementRepository
{
    private static readonly Dictionary<SqlRepoKey, string> _sqlDictionary = new(100);

    static SqlStatementRepository()
    {
        foreach (var kvp in GetApiTransactionSql()
            .Union(GetLoggingSql())
            .Union(GetEodPriceSql())
            .Union(GetAdjustedEodPriceSql())
            .Union(GetRemoteFileSql())
            .Union(GetSplitsSql())
            .Union(GetDividendsSql())
            .Union(GetFundamentalsSql())
            .Union(GetBacktestSql())
            .Union(GetMigrationsSql()))
        {
            _ = _sqlDictionary.TryAdd(kvp.Key, kvp.Value);
        }
    }

    public static IDictionary<string, string> BuildDictionary(DatabaseEngine dbEngine,
        bool allowMissingKeys = false)
    {
        var results = _sqlDictionary.Where(d => d.Key.DbEngine.Equals(dbEngine))
            .Select(k => new KeyValuePair<string, string>(k.Key.SqlStatementKey, k.Value)).ToDictionary();

        if (!allowMissingKeys)
        {
            var missingKeys = SqlKeys.GetKeys().Except(results.Keys).ToArray();

            if (missingKeys.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append(nameof(SqlStatementRepository));
                sb.Append('.');
                sb.Append(nameof(BuildDictionary));
                sb.Append($" missing keys for: {string.Join(", ", missingKeys)}");
                throw new Exception(sb.ToString());
            }
        }

        return results;
    }
}

internal readonly struct SqlRepoKey
{
    public SqlRepoKey(string sqlStatementKey, DatabaseEngine dbEngine = DatabaseEngine.PostgreSql)
    {
        SqlStatementKey = sqlStatementKey ?? throw new ArgumentNullException(nameof(sqlStatementKey));
        DbEngine = dbEngine;
    }

    public DatabaseEngine DbEngine { get; init; } = DatabaseEngine.PostgreSql;
    public string SqlStatementKey { get; init; }
}