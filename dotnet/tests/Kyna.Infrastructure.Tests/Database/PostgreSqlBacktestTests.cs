using Kyna.Analysis.Technical;
using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlBacktestTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly Guid? _processId = Guid.NewGuid();

    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlBacktestTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_Backtest_InternalTransaction()
    {
        var backtest = CreateBacktest();

        using var context = _fixture.Backtests.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertBacktest), backtest);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.FetchBacktest, "id = @Id");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<Backtest>(sql, backtest);
        Assert.NotNull(actual);
        Assert.Equal(backtest, actual);
    }

    [Fact]
    public void InsertAndFetch_BacktestResult_InternalTransaction()
    {
        var backtest = CreateBacktest();
        var backtestResult = CreateBacktestResult(backtest);

        using var context = _fixture.Backtests.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertBacktestResult), backtestResult);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.FetchBacktestResult, "id = @Id");
        var actual = context.QueryFirstOrDefault<BacktestResult>(sql, backtestResult);
        Assert.NotNull(actual);
        Assert.Equal(backtestResult, actual);
    }

    [Fact]
    public void InsertAndFetch_BacktestStats_InternalTransaction()
    {
        using var context = _fixture.Backtests.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.DeleteBacktestStats));
        var backtestStats = CreateBacktestStats();

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertBacktestStats), backtestStats);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.FetchBacktestStats, "backtest_id = @BacktestId");
        var actual = context.QueryFirstOrDefault<BacktestStats>(sql, backtestStats);
        Assert.NotNull(actual);
        Assert.Equal(backtestStats, actual);
    }

    private Backtest CreateBacktest()
    {
        return new Backtest(Guid.NewGuid(), "Test", "Test", "polygon.io", "Integration Test",
            PricePoint.Close.GetEnumDescription(),
            0.1D, PricePoint.High.GetEnumDescription(),
            0.1D, PricePoint.Low.GetEnumDescription(),
            _processId);
    }

    private static BacktestResult CreateBacktestResult(Backtest backtest)
    {
        return new BacktestResult(Guid.NewGuid(), backtest.Id, "Test Signal", "Test.US", null, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            PricePoint.Close.GetEnumDescription(),
            10M, DateOnly.FromDateTime(DateTime.UtcNow),
            backtest.TargetUpPricePoint, 11M,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            backtest.TargetDownPricePoint,
            9M, "Down", 4, 4);
    }

    private static BacktestStats CreateBacktestStats()
    {
        return new BacktestStats(Guid.NewGuid(),
            "Test", "Test Signal", "Category", "Sub Category",
            100, 10, 50D, "Type 1", 40, 56, Guid.NewGuid());
    }
}
