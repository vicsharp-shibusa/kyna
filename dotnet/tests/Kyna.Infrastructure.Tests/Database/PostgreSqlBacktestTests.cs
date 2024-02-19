using Kyna.Analysis.Technical;
using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlBacktestTests
{
    private PostgreSqlContext? _context;
    private const string DbName = "Backtests";
    private readonly Guid? _processId = Guid.NewGuid();

    public PostgreSqlBacktestTests()
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
    public void InsertAndFetch_Backtest_InternalTransaction()
    {
        var backtest = CreateBacktest();

        _context!.Execute(_context.Sql.Backtests.UpsertBacktest, backtest);

        string sql = @$"{_context.Sql.Backtests.FetchBacktest} WHERE id = @Id";
        var actual = _context.QueryFirstOrDefault<Backtest>(sql, backtest);
        Assert.NotNull(actual);
        Assert.Equal(backtest, actual);
    }

    [Fact]
    public void InsertAndFetch_BacktestResult_InternalTransaction()
    {
        var backtest = CreateBacktest();
        var backtestResult = CreateBacktestResult(backtest);

        _context!.Execute(_context.Sql.Backtests.UpsertBacktestResult, backtestResult);

        string sql = @$"{_context.Sql.Backtests.FetchBacktestResult} WHERE id = @Id";
        var actual = _context.QueryFirstOrDefault<BacktestResult>(sql, backtestResult);
        Assert.NotNull(actual);
        Assert.Equal(backtestResult, actual);
    }

    private static Backtest CreateBacktest()
    {
        return new Backtest(Guid.NewGuid(), "Test", "Test", "eodhd.com", "Integration Test",
            PricePoint.Close.GetEnumDescription(),
            0.1D, PricePoint.High.GetEnumDescription(),
            0.1D, PricePoint.Low.GetEnumDescription(),
            DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks);

    }

    private BacktestResult CreateBacktestResult(Backtest backtest)
    {
        return new BacktestResult(Guid.NewGuid(), backtest.Id, "Test.US",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            PricePoint.Close.GetEnumDescription(),
            10M, DateOnly.FromDateTime(DateTime.UtcNow),
            backtest.TargetUpPricePoint, 11M,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            backtest.TargetDownPricePoint,
            9M, "Down", 4, 4,
            DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId);
    }
}
