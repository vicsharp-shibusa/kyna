using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlApiTransactionTests
{
    private readonly SqlRepository _postgreSqlRepo = new(DatabaseEngine.PostgreSql);

    private const string _dateTimeEquality = "yyyyMMddHHmmssfff";

    private PostgreSqlContext? _context;

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

        _context = new PostgreSqlContext(configuration.GetConnectionString("Imports"));
    }

    [Fact]
    public void InsertAndFetch_ApiTransaction_InternalTransaction()
    {
        var transactionDao = CreateApiTransaction();

        _context!.Execute(_postgreSqlRepo.InsertApiTransaction, transactionDao);

        string sql = $"{_postgreSqlRepo.FetchApiTransaction} WHERE sub_category = @SubCategory";

        var actual = _context.QueryFirstOrDefault<Infrastructure.Database.DataAccessObjects.ApiTransaction>(
            sql, new { transactionDao.SubCategory });

        Assert.NotNull(actual);
        Assert.Equal(transactionDao.TimestampUtc.ToString(_dateTimeEquality),
            actual.TimestampUtc.ToString(_dateTimeEquality));
        Assert.Equal(transactionDao.Source, actual.Source);
        Assert.Equal(transactionDao.Category, actual.Category);
        Assert.Equal(transactionDao.SubCategory, actual.SubCategory);
        Assert.Equal(transactionDao.RequestUri, actual.RequestUri);
        Assert.Equal(transactionDao.RequestMethod, actual.RequestMethod);
        Assert.Equal(transactionDao.RequestPayload, actual.RequestPayload);
        Assert.Equal(transactionDao.RequestHeaders, actual.RequestHeaders);
        Assert.Equal(transactionDao.ResponseHeaders, actual.ResponseHeaders);
        Assert.Equal(transactionDao.ResponseStatusCode, actual.ResponseStatusCode);
        Assert.Equal(transactionDao.ResponseBody, actual.ResponseBody);
    }

    private static Infrastructure.Database.DataAccessObjects.ApiTransaction CreateApiTransaction()
    {
        return new Infrastructure.Database.DataAccessObjects.ApiTransaction()
        {
            Source = "Test",
            Category = "Price Action",
            SubCategory = Guid.NewGuid().ToString("N")[..8],
            RequestHeaders = "[]",
            RequestMethod = "GET",
            RequestPayload = null,
            RequestUri = "https://test.com/api/AAPL",
            ResponseBody = "{\"stuff\":\"stuff value\"}",
            ResponseHeaders = "[]",
            ResponseStatusCode = "200"
        };
    }
}
