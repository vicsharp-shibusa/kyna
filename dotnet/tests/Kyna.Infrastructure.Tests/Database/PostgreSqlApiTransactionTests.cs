using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlApiTransactionTests
{
    private PostgreSqlContext? _context;
    private const string DbName = "Imports";

    public PostgreSqlApiTransactionTests()
    {
        Configure();
        Debug.Assert(_context != null);
    }

    private void Configure()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();

        Debug.Assert(configuration != null);

        _context = new PostgreSqlContext(new DbDef(DbName, DatabaseEngine.PostgreSql, configuration.GetConnectionString(DbName)!));
    }

    [Fact]
    public void InsertAndFetch_ApiTransaction_InternalTransaction()
    {
        var transactionDao = CreateApiTransaction(Guid.NewGuid().ToString());

        _context!.Execute(_context.Sql.ApiTransactions.Insert, transactionDao);

        string sql = $"{_context.Sql.ApiTransactions.Fetch} WHERE sub_category = @SubCategory";

        var actual = _context.QueryFirstOrDefault<ApiTransaction>(
            sql, new { transactionDao.SubCategory });

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

        _context!.Execute(_context.Sql.ApiTransactions.Insert, apiTransactions);

        string sql = "SELECT MAX(id) FROM api_transactions where sub_category = @Sub";

        int maxOneId = _context.QueryFirstOrDefault<int>(sql, new { Sub = "One" });
        int maxTwoId = _context.QueryFirstOrDefault<int>(sql, new { Sub = "Two" });

        string[] categories = ["Price Action"];

        var itemsToMigrate = _context.Query<ApiTransactionForMigration>(
                $"{_context.Sql.ApiTransactions.FetchForMigration} WHERE source = @Source AND category {_context.Sql.GetInCollectionSql("Categories")}",
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
