using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Data;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlApiTransactionTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlApiTransactionTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_ApiTransaction_InternalTransaction()
    {
        var transactionDao = CreateApiTransaction(Guid.NewGuid().ToString());

        using var context = _fixture.Imports.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Imports.GetSql(SqlKeys.InsertApiTransaction), transactionDao);

        var sql = _fixture.Imports.GetSql(SqlKeys.FetchApiTransaction, "sub_category = @SubCategory");

        var actual = context.QueryFirstOrDefault<ApiTransaction>(sql, new { transactionDao.SubCategory });

        Assert.NotNull(actual);
        Assert.Equal(transactionDao, actual);
    }

    [Fact]
    public void FindTransactionsForMigration_GroupBy()
    {
        const int Count = 10;
        ApiTransaction[] apiTransactions = new ApiTransaction[Count];

        for (int i = 0; i < 5; i++)
        {
            apiTransactions[i] = CreateApiTransaction("One");
        }
        for (int i = 5; i < Count; i++)
        {
            apiTransactions[i] = CreateApiTransaction("Two");
        }

        using var context = _fixture.Imports.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Imports.GetSql(SqlKeys.InsertApiTransaction), apiTransactions);

        string sql = "SELECT MAX(id) FROM api_transactions where sub_category = @Sub";

        int maxOneId = context.QueryFirstOrDefault<int>(sql, new { Sub = "One" });
        int maxTwoId = context.QueryFirstOrDefault<int>(sql, new { Sub = "Two" });

        string[] categories = ["Price Action"];

        var itemsToMigrate = context.Query<ApiTransactionForMigration>(
            _fixture.Imports.GetSql(SqlKeys.FetchApiTransactionsForMigration, "source = @Source",
            $"category {SqlFactory.GetSqlSyntaxForInCollection("Categories")}"),
                new { Source = "Test", categories });

        Assert.NotEmpty(itemsToMigrate);

        var items = itemsToMigrate.GroupBy(g => g.SubCategory).Select(g => new
        {
            SubCategory = g.Key,
            Item = g.MaxBy(i => i.Id)
        });

        Assert.NotEmpty(items);

        Assert.NotNull(items.FirstOrDefault()?.Item);

        int? oneId = items.FirstOrDefault(i => i.SubCategory == "One")?.Item?.Id;
        int? twoId = items.FirstOrDefault(i => i.SubCategory == "Two")?.Item?.Id;

        Assert.NotNull(oneId);
        Assert.NotNull(twoId);

        Assert.Equal(maxOneId, oneId);
        Assert.Equal(maxTwoId, twoId);
    }

    private static ApiTransaction CreateApiTransaction(string? subCategory = "Sub",
        Guid? processId = null)
    {
        return new ApiTransaction()
        {
            Source = "Test",
            Category = "Price Action",
            SubCategory = subCategory ?? "Sub",
            RequestHeaders = "[]",
            RequestMethod = "GET",
            RequestPayload = null,
            RequestUri = "https://test.com/api/AAPL",
            ResponseBody = "{\"stuff\":\"stuff value\"}",
            ResponseHeaders = "[]",
            ResponseStatusCode = "200",
            ProcessId = processId ?? Guid.NewGuid()
        };
    }
}
