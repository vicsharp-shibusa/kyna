using Kyna.Infrastructure.Database;

namespace Kyna.Infrastructure.Tests.Database;

public class SqlFactoryWhereClauseTests
{
    [Fact]
    public void WhereClause_None_NoWhereInOutput()
    {
        const string Key = "test";
        const string BaseSql = "SELECT * FROM table";

        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,BaseSql }
        });

        var sql = sut.GetFormattedSqlWithWhereClause(Key);

        Assert.Equal(BaseSql, sql);
    }

    [Fact]
    public void WhereClause_SemiColonInSql_SemiColonRemoved()
    {
        const string BaseSql = @"SELECT *
FROM
table;"; // <- this is the semicolon being removed.
        const string Expected = "SELECT * FROM table WHERE x = 'test'";

        var sut = new SqlCollection(new Dictionary<string, string>() {
            {"test",BaseSql }
        });

        var sql = sut.GetFormattedSqlWithWhereClause("test", LogicalOperator.And, "x = 'test'");

        Assert.Equal(Expected, sql);
    }

    [Theory]
    [InlineData("x = 'test';")]
    [InlineData("(x = 'test';)")]
    [InlineData("((x = 'test';))")]
    public void WhereClause_SemiColonInWhere_SemiColonRemoved(string clause)
    {
        const string Key = "test";
        const string BaseSql = @"SELECT * FROM table";
        const string Expected = "SELECT * FROM table WHERE x = 'test'";

        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,BaseSql }
        });

        var sql = sut.GetFormattedSqlWithWhereClause(Key, LogicalOperator.And, clause);

        Assert.Equal(Expected, sql);
    }

    [Fact]
    public void WhereClause_ContainsConditional_WrappedInParens()
    {
        const string Key = "test";
        const string BaseSql = @"SELECT * FROM table";
        const string Expected = "SELECT * FROM table WHERE (p = 1 AND x = 'test')";

        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,BaseSql }
        });

        var sql = sut.GetFormattedSqlWithWhereClause(Key, LogicalOperator.And, "(p = 1 AND x = 'test')");

        Assert.Equal(Expected, sql);
    }

    /// <summary>
    /// Even though an "OR" is found in the where clause, the clause is not wrapped in parens
    /// because the "OR" is found inside a literal.
    /// </summary>
    [Fact]
    public void WhereClause_IgnoresConditionalsInLiterals()
    {
        const string Key = "test";
        const string BaseSql = @"SELECT * FROM table";
        const string Expected = "SELECT * FROM table WHERE x = 'test OR other test'";

        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,BaseSql }
        });

        var sql = sut.GetFormattedSqlWithWhereClause(Key, LogicalOperator.And, "x = 'test OR other test'");

        Assert.Equal(Expected, sql);
    }
}

