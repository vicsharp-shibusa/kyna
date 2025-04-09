namespace Kyna.Infrastructure.Database;

public interface ISqlRepository
{
    string? GetFormattedSqlWithWhereClause(string key, LogicalOperator logOp = LogicalOperator.And, params string[] whereClauses);
    string? GetSql(string key, bool formatSql = false);
    string? GetSql(string key, LogicalOperator logOp, params string[] whereClauseKeys);
    string? GetSql(string key, params string[] whereClauseKeys);
    bool TryGetSql(string key, out string? statement, bool formatSql = false);
    bool TryGetSqlWithWhereClause(string key, out string? statement, LogicalOperator logOp = LogicalOperator.And, params string[] whereClauses);
}

