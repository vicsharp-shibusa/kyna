using Kyna.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace Kyna.Infrastructure.Database;

internal sealed class SqlFactory : ISqlRepository
{
    private readonly Dictionary<string, string> _sqlStatements;

    public SqlFactory(DatabaseEngine engine) : this(SqlRepository.BuildDictionary(engine))
    {
    }

    internal SqlFactory(IDictionary<string, string> sqlStatements)
    {
        _sqlStatements = new Dictionary<string, string>(
            sqlStatements ?? throw new ArgumentNullException(nameof(sqlStatements)),
            StringComparer.OrdinalIgnoreCase
        );

        if (_sqlStatements.Count == 0)
        {
            throw new ArgumentException($"At least one key/value pair of SQL statements is required to construct a {nameof(SqlFactory)} instance");
        }
    }

    public string? GetSql(string key, bool formatSql = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_sqlStatements.TryGetValue(key, out var value))
            return null;
        return formatSql ? FormatSql(value) : value;
    }

    public string? GetSql(string key, params string[] whereClauseKeys) =>
        GetSql(key, LogicalOperator.And, whereClauseKeys);

    public string? GetSql(string key, LogicalOperator logOp, params string[] whereClauseKeys)
    {
        if (whereClauseKeys.Length == 0)
            return GetSql(key, formatSql: true);

        var finalClauses = whereClauseKeys.Select(k => FormatWhereClause(k))
                .Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();

        StringBuilder result = new(GetSql(key, true));

        if (finalClauses.Length > 0)
        {
            result.Append(" WHERE ");
            result.Append(string.Join($" {logOp.GetEnumDescription()} ", finalClauses));
        }

        return result.ToString();
    }

    public bool TryGetSql(string key, out string? statement, bool formatSql = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_sqlStatements.TryGetValue(key, out var value))
        {
            statement = formatSql ? FormatSql(value) : value;
            return true;
        }
        statement = null;
        return false;
    }

    public string? GetFormattedSqlWithWhereClause(string key, LogicalOperator logOp = LogicalOperator.And,
        params string[] whereClauses)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_sqlStatements.TryGetValue(key, out var sql))
            return null;

        sql = FormatSql(sql, removeEndingSemicolon: true);

        if (whereClauses.Length == 0)
            return sql;

        var formattedClauses = whereClauses.Select(FormatWhereClause)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToArray();

        if (formattedClauses.Length == 0)
        {
            return sql;
        }
        else if (formattedClauses.Length == 1)
        {
            return $"{sql} WHERE {formattedClauses.First()}";
        }

        sql = sql.Trim();
        while (sql.EndsWith(';'))
            sql = sql[..^1];

        return $"{sql} WHERE {string.Join($" {logOp.ToString().ToUpper()} ",
            formattedClauses)}";
    }

    public static string GetSqlSyntaxForInCollection(string parameterName, DatabaseEngine dbEngine = DatabaseEngine.PostgreSql)
    {
        ArgumentNullException.ThrowIfNull(parameterName);
        parameterName = parameterName.Trim();
        if (parameterName.Length == 0)
            throw new ArgumentException("Cannot be empty.", nameof(parameterName));

        if (!parameterName.StartsWith('@'))
        {
            parameterName = $"@{parameterName}";
        }

        return dbEngine switch
        {
            DatabaseEngine.PostgreSql => $" = ANY({parameterName})",
            DatabaseEngine.None => throw new Exception($"No database engine specified in {nameof(SqlFactory)}.{nameof(GetSqlSyntaxForInCollection)}"),
            _ => $" IN {parameterName}"
        };
    }

    private static string FormatSql(string sql, bool removeEndingSemicolon = false)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "";

        sql = sql.Replace(Environment.NewLine, " ").Trim();

        var outputArray = new char[sql.Length];
        int r = 0;
        int spaceCount = 0;
        var inLiteral = false;
        for (int k = 0; k < sql.Length; k++)
        {
            char c = sql[k];

            if (c == '\'')
            {
                inLiteral = !inLiteral;
                outputArray[r++] = c;
                continue;
            }

            if (inLiteral)
            {
                outputArray[r++] = c;
                continue;
            }
            else if (c == ' ')
            {
                if (spaceCount > 0)
                {
                    continue;
                }
                spaceCount = 1;
            }
            else
                spaceCount = 0;
            outputArray[r++] = c;
        }

        Array.Resize(ref outputArray, r);
        var result = new string(outputArray).Trim();

        if (removeEndingSemicolon)
            while (result.Length > 0 && result.EndsWith(';'))
                result = result[..^1].Trim();

        return result;
    }

    private static readonly Regex _sqlLiteralsRegex = new(@"'(?:[^']|'')*'");

    private static string FormatWhereClause(string clause)
    {
        if (string.IsNullOrWhiteSpace(clause))
            return "";

        clause = clause.Trim();
        while (clause.StartsWith('(') && clause.EndsWith(')'))
        {
            clause = clause[1..^1].Trim();
        }

        while (clause.Length > 0 && clause[^1] == ';')
            clause = clause[..^1].TrimEnd();

        if (clause.Length == 0)
            return "";

        bool containsConditionals = false;

        /*
         * A "tick" is short-hand for a single quote (i.e., ').
         */
        var tickCount = clause.ToCharArray().Count(c => c == '\'');
        if (tickCount > 0)
        {
            var matches = _sqlLiteralsRegex.Matches(clause);

            if (matches.Count > 0)
            {
                var key = new byte[8];
                var d = new Dictionary<string, string>(matches.Count);

                // strip away the literals and replace them with random strings.
                // Use the random string as the key and the original value as the value
                // in the dictionary.
                foreach (Match m in matches)
                {
                    Random.Shared.NextBytes(key);
                    var k = Convert.ToBase64String(key);
                    d.Add(k, m.Value);
                    clause = clause.Replace(m.Value, k);
                }

                // Now any conditional operations can be seen without the literals creating false positives.
                containsConditionals = clause.Contains(" and ", StringComparison.OrdinalIgnoreCase) ||
                    clause.Contains(" or ", StringComparison.OrdinalIgnoreCase);

                // put the original values back.
                foreach (var kvp in d)
                {
                    clause = clause.Replace(kvp.Key, kvp.Value);
                }
            }
        }

        // wrap any clause with conditional with '()'
        if (containsConditionals)
            clause = $"({clause})";

        return clause;
    }
}

