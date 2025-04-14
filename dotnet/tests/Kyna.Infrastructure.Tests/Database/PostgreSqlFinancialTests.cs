using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlFinancialTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly Guid? _processId = Guid.NewGuid();

    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlFinancialTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_EodPrices()
    {
        Guid processId = Guid.NewGuid();
        var eodPriceDao = CreateEodPricesDao(processId);

        using var context = _fixture.Financials.GetConnection();
        Assert.NotNull(context);

        var sqlDelete = _fixture.Financials.Sql.GetSql(SqlKeys.DeleteEodPrices,
            "source = @Source", "code = @Code", "date_eod = @DateEod");

        context.Execute(sqlDelete, eodPriceDao);
        context.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.UpsertEodPrice), eodPriceDao);

        var sql = _fixture.Financials.Sql.GetSql(SqlKeys.FetchEodPrices, "process_id = @ProcessId");

        var actual = context.QueryFirstOrDefault<EodPrice>(sql, new { eodPriceDao.ProcessId });

        Assert.Equal(eodPriceDao, actual);
    }

    [Fact]
    public void InsertAndFetch_AdjustedEodPrices()
    {
        Guid processId = Guid.NewGuid();
        var eodPriceDao = CreateAdjustedEodPricesDao(processId);

        var sqlDelete = _fixture.Financials.Sql.GetSql(SqlKeys.DeleteEodAdjustedPrices,
            "source = @Source", "code = @Code", "date_eod = @DateEod");

        using var context = _fixture.Financials.GetConnection();
        Assert.NotNull(context);

        context.Execute(sqlDelete, eodPriceDao);
        context.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.UpsertEodAdjustedPrice), eodPriceDao);

        var sql = _fixture.Financials.Sql.GetSql(SqlKeys.FetchEodAdjustedPrices, "process_id = @ProcessId");

        var actual = context.QueryFirstOrDefault<EodAdjustedPrice>(
            sql, new { eodPriceDao.ProcessId });

        Assert.Equal(eodPriceDao, actual);
    }

    [Fact]
    public void InsertAndFetch_Splits()
    {
        Guid processId = Guid.NewGuid();
        var splitDao = CreateSplitDao(processId);

        using var context = _fixture.Financials.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.DeleteSplitsForSource), splitDao);
        context.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.UpsertSplit), splitDao);

        var sql = _fixture.Financials.Sql.GetSql(SqlKeys.FetchSplits, "process_id = @ProcessId");

        var actual = context.QueryFirstOrDefault<Split>(sql, new { splitDao.ProcessId });
        Assert.NotNull(actual);
        Assert.Equal(splitDao, actual);
    }

    [Fact]
    public void InsertAndFetch_Dividends()
    {
        Guid processId = Guid.NewGuid();
        var dividendDao = CreateDividendDao(processId);

        using var context = _fixture.Financials.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.DeleteDividendsForSource), dividendDao);
        context.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.UpsertDividend), dividendDao);

        var sql = _fixture.Financials.Sql.GetSql(SqlKeys.FetchDividends, "process_id = @ProcessId");

        var actual = context.QueryFirstOrDefault<Dividend>(sql, new { dividendDao.ProcessId });
        Assert.NotNull(actual);
        Assert.Equal(dividendDao, actual);
    }

    [Fact]
    public void InsertAndFetch_BasicEntity()
    {
        var entity = new Entity("BTest", "BTEST.US");

        using var context = _fixture.Financials.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.DeleteEntityForSourceAndCode),
            new { entity.Source, entity.Code });

        context.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.InsertBasicEntity), entity);

        var entities = context.Query<Entity>(_fixture.Financials.Sql.GetSql(SqlKeys.FetchEntity));

        Assert.NotNull(entities);
        Assert.NotEmpty(entities);
        Assert.Contains(entity, entities);
    }

    [Fact]
    public void InsertAndFetch_Entities()
    {
        var entity = CreateEntity();

        using var context = _fixture.Financials.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.DeleteEntityForSourceAndCode),
            new { entity.Source, entity.Code });

        context.Execute(_fixture.Financials.Sql.GetSql(SqlKeys.UpsertEntity), entity);

        var entities = context.Query<Entity>(_fixture.Financials.Sql.GetSql(SqlKeys.FetchEntity));

        Assert.NotNull(entities);
        Assert.NotEmpty(entities);
        Assert.Contains(entity, entities);
    }

    private static Entity CreateEntity()
    {
        return new Entity("Test", "TEST.US")
        {
            Type = "Common Stock",
            Name = "Test Company",
            Exchange = "NYSE",
            Country = "USA",
            Currency = "USD",
            GicGroup = "GR1",
            GicIndustry = "Test Industry",
            GicSector = "Test Sector",
            GicSubIndustry = "Test SubIndustry",
            Industry = "Test Industry",
            Phone = "(555) 555-1232",
            WebUrl = "https//test-company.io",
            Sector = "Test Sectory",
        };
    }

    private static EodPrice CreateEodPricesDao(Guid? processId = null)
    {
        var (Open, High, Low, Close, Volume) = StockPriceGenerator.GenerateRandomStockPrice(50M, 200M);

        return new EodPrice("TEST", "TEST.US", processId ?? Guid.NewGuid())
        {
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume,
            DateEod = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }

    private static EodAdjustedPrice CreateAdjustedEodPricesDao(Guid? processId = null)
    {
        var (Open, High, Low, Close, Volume) = StockPriceGenerator.GenerateRandomStockPrice(50M, 200M);

        return new EodAdjustedPrice("TEST", "TEST.US", processId ?? Guid.NewGuid())
        {
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume,
            DateEod = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }

    private static Split CreateSplitDao(Guid? processId = null)
    {
        return new Split("TEST", "TEST.US", processId ?? Guid.NewGuid())
        {
            After = 1,
            Before = 2,
            SplitDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }

    private static Dividend CreateDividendDao(Guid? processId = null)
    {
        DateOnly dt = DateOnly.FromDateTime(DateTime.UtcNow);
        return new Dividend("TEST", "TEST.US", "TEST", processId ?? Guid.NewGuid())
        {
            DeclarationDate = dt,
            ExDividendDate = dt,
            PayDate = dt,
            RecordDate = dt,
            Amount = 10M,
            Frequency = 4
        };
    }
}

internal static class StockPriceGenerator
{
    private static readonly Random _random = new(Guid.NewGuid().GetHashCode());

    public static (decimal Open, decimal High, decimal Low, decimal Close, long Volume) GenerateRandomStockPrice(decimal minPrice, decimal maxPrice)
    {
        decimal open = GenerateRandomPrice(minPrice, maxPrice);
        decimal close = GenerateRandomPrice(minPrice, maxPrice);
        decimal high = Math.Max(open, close);
        decimal low = Math.Min(open, close);

        return (open, high, low, close, _random.NextInt64(1_000_000, 100_000_000));
    }

    private static decimal GenerateRandomPrice(decimal minPrice, decimal maxPrice)
    {
        return minPrice + (maxPrice - minPrice) * (decimal)_random.NextDouble();
    }
}
