using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlResearchTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly Guid? _processId = Guid.NewGuid();

    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlResearchTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_StatsBuild()
    {
        var statsBuild = CreateStatsBuild();

        using var context = _fixture.Backtests.GetConnection();
        Assert.NotNull(context);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertStatsBuild), statsBuild);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectStatsBuild, "id = @Id");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<StatsBuild>(sql, statsBuild);
        Assert.NotNull(actual);
        Assert.Equal(statsBuild, actual);
    }

    [Fact]
    public void InsertAndFetch_StatsDetail()
    {
        var statsDetail = CreateStatsDetail();

        using var context = _fixture.Backtests.GetConnection();
        Assert.NotNull(context);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertStatsDetail), statsDetail);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectStatsDetail,
            "stats_build_id = @StatsBuildId",
            "code = @Code",
            "stat_type = @StatType",
            "stat_key = @StatKey");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<StatsDetail>(sql, statsDetail);
        Assert.NotNull(actual);
        Assert.Equal(statsDetail, actual);
    }

    [Fact]
    public void InsertAndFetch_Stat()
    {
        var stat = CreateStat();

        using var context = _fixture.Backtests.GetConnection();
        Assert.NotNull(context);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertStat), stat);

        //   PRIMARY KEY (stats_build_id, category, sub_category, stat_type, stat_key)
        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectStat,
            "stats_build_id = @StatsBuildId",
            "category = @Category",
            "sub_category = @SubCategory",
            "stat_type = @StatType",
            "stat_key = @StatKey");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<Stat>(sql, stat);
        Assert.NotNull(actual);
        Assert.Equal(stat, actual);
    }

    //[Fact]
    //public void InsertAndFetch_Backtest_InternalTransaction()
    //{
    //    var backtest = CreateBacktest();

    //    using var context = _fixture.Backtests.GetConnection();
    //    Debug.Assert(context != null);

    //    context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertBacktest), backtest);

    //    var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectBacktest, "id = @Id");
    //    Assert.NotNull(sql);
    //    var actual = context.QueryFirstOrDefault<Backtest>(sql, backtest);
    //    Assert.NotNull(actual);
    //    Assert.Equal(backtest, actual);
    //}

    //[Fact]
    //public void InsertAndFetch_BacktestResult_InternalTransaction()
    //{
    //    var backtest = CreateBacktest();
    //    var backtestResult = CreateBacktestResult(backtest);

    //    using var context = _fixture.Backtests.GetConnection();
    //    Debug.Assert(context != null);

    //    context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertBacktestResult), backtestResult);

    //    var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectBacktestResult, "id = @Id");
    //    var actual = context.QueryFirstOrDefault<BacktestResult>(sql, backtestResult);
    //    Assert.NotNull(actual);
    //    Assert.Equal(backtestResult, actual);
    //}

    //[Fact]
    //public void InsertAndFetch_BacktestStats_InternalTransaction()
    //{
    //    using var context = _fixture.Backtests.GetConnection();
    //    Debug.Assert(context != null);

    //    context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.DeleteBacktestStats));
    //    var backtestStats = CreateBacktestStats();

    //    context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertBacktestStats), backtestStats);

    //    var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectBacktestStats, "backtest_id = @BacktestId");
    //    var actual = context.QueryFirstOrDefault<BacktestStats>(sql, backtestStats);
    //    Assert.NotNull(actual);
    //    Assert.Equal(backtestStats, actual);
    //}

    private StatsBuild CreateStatsBuild()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new StatsBuild(_processId)
        {
            ConfigContent = "{}",
            CreatedAt = now,
            UpdatedAt = now,
            Id = Guid.NewGuid(),
            Source = "test",
        };
    }

    private StatsDetail CreateStatsDetail()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new StatsDetail(_processId)
        {
            Code = "test",
            CreatedAt = now,
            UpdatedAt = now,
            StatKey = "key",
            StatMeta = "meta",
            StatsBuildId = Guid.NewGuid(),
            StatType = "type",
            StatVal = 1.0
        };
    }

    private Stat CreateStat()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Stat(_processId)
        {
            Category = "category",
            ConfidenceLower = 0.5,
            ConfidenceUpper = 0.8,
            CreatedAt = now,
            UpdatedAt = now,
            SampleSize = 100,
            SearchSize = 200,
            StatsBuildId = Guid.NewGuid(),
            StatKey = "key",
            StatType = "type",
            SubCategory = "sub-category",
            StatVal = 0.9
        };
    }
    //private static BacktestResult CreateBacktestResult(Backtest backtest)
    //{
    //    return new BacktestResult(Guid.NewGuid(), backtest.Id, "Test Signal", "Test.US", null, null,
    //        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
    //        PricePoint.Close.GetEnumDescription(),
    //        10M, DateOnly.FromDateTime(DateTime.UtcNow),
    //        backtest.TargetUpPricePoint, 11M,
    //        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
    //        backtest.TargetDownPricePoint,
    //        9M, "Down", 4, 4);
    //}

    //private static BacktestStats CreateBacktestStats()
    //{
    //    return new BacktestStats(Guid.NewGuid(),
    //        "Test", "Test Signal", "Category", "Sub Category",
    //        100, 10, 50D, "Type 1", 40, 56, Guid.NewGuid());
    //}
}
