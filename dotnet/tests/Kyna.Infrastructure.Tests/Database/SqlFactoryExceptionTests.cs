#nullable disable

using Kyna.Infrastructure.Database;

namespace Kyna.Infrastructure.Tests.Database;

public class SqlFactoryExceptionTests
{
    [Fact]
    public void Ctor_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlCollection(null));
    }

    [Fact]
    public void Ctor_EmptyInput_Throws()
    {
        var sut = new Dictionary<string, string>();

        Assert.Throws<ArgumentException>(() => new SqlCollection(sut));
    }

    [Fact]
    public void GetSqlStatement_NullKey_Throws()
    {
        const string Key = "test";
        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,"SELECT * FROM table" }
        });

        Assert.Throws<ArgumentNullException>(() => sut.GetSql(null));
    }

    [Fact]
    public void TryGetSqlStatement_NullKey_Throws()
    {
        const string Key = "test";
        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,"SELECT * FROM table" }
        });

        Assert.Throws<ArgumentNullException>(() => sut.TryGetSql(null, out var _));
    }

    [Fact]
    public void GetSqlStatementWithWhereClause_NullKey_Throws()
    {
        const string Key = "test";
        var sut = new SqlCollection(new Dictionary<string, string>() {
            {Key,"SELECT * FROM table" }
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = sut.GetFormattedSqlWithWhereClause(null, LogicalOperator.And, "x = 'test'");
        });
    }
}
