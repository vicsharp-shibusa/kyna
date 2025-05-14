using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Diagnostics;
using Xunit;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlStatsTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly Guid? _processId = Guid.NewGuid();
    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlStatsTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_StatsBuild_InternalTransaction()
    {
        var statsBuild = CreateStatsBuild();

        using var context = _fixture.Backtests.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertStatsBuild), statsBuild);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectStatsBuild, "id = @Id");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<StatsBuild>(sql, statsBuild);
        Assert.NotNull(actual);
        Assert.Equal(statsBuild, actual);
    }

    [Fact]
    public void InsertAndFetch_StatsDetail_InternalTransaction()
    {
        var statsDetail = CreateStatsDetail();

        using var context = _fixture.Backtests.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertStatsDetail), statsDetail);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectStatsDetail,
            "stats_build_id = @StatsBuildId AND code = @Code AND stat_type = @StatType AND stat_key = @StatKey");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<StatsDetail>(sql, statsDetail);
        Assert.NotNull(actual);
        Assert.Equal(statsDetail, actual);
    }

    [Fact]
    public void InsertAndFetch_Stats_InternalTransaction()
    {
        var stats = CreateStats();

        using var context = _fixture.Backtests.GetConnection();
        Debug.Assert(context != null);

        context.Execute(_fixture.Backtests.Sql.GetSql(SqlKeys.UpsertStat), stats);

        var sql = _fixture.Backtests.Sql.GetSql(SqlKeys.SelectStat,
            "stats_build_id = @StatsBuildId AND category = @Category AND sub_category = @SubCategory AND stat_type = @StatType AND stat_key = @StatKey");
        Assert.NotNull(sql);
        var actual = context.QueryFirstOrDefault<Stat>(sql, stats);
        Assert.NotNull(actual);
        Assert.Equal(stats, actual);
    }

    private static StatsBuild CreateStatsBuild()
    {
        return new StatsBuild()
        {
            Id = Guid.NewGuid(),
            Source = "Polygon.io",
            ConfigContent = @"{""market"":""SPY"", ""lookback"": 20}"
        };
    }

    private static StatsDetail CreateStatsDetail()
    {
        return new StatsDetail()
        {
            StatsBuildId = Guid.NewGuid(),
            Code = "SPY",
            StatType = "signal",
            StatKey = "bearish_engulfing",
            StatVal = 1.0,
            StatMeta = @"{""date"":""2025-04-01"", ""trend"":0.3, ""holding_period"":5}"
        };
    }

    private static Stat CreateStats()
    {
        return new Stat()
        {
            StatsBuildId = Guid.NewGuid(),
            Category = "Technology",
            SubCategory = "ETFs",
            StatType = "signal",
            StatKey = "bearish_engulfing",
            StatVal = 0.51,
            SearchSize = 500,
            SampleSize = 100,
            ConfidenceLower = 0.41,
            ConfidenceUpper = 0.61
        };
    }
}