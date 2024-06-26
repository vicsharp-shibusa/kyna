﻿using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlFinancialTests
{
    private PostgreSqlContext? _context;
    private const string DbName = "Financials";

    public PostgreSqlFinancialTests()
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
    public void InsertAndFetch_EodPrices()
    {
        Guid processId = Guid.NewGuid();
        var eodPriceDao = CreateEodPricesDao(processId);

        string sqlDelete = $@"{_context!.Sql.EodPrices.Delete} WHERE
source = @Source AND code = @Code AND date_eod = @DateEod";

        _context.Execute(sqlDelete, eodPriceDao);
        _context.Execute(_context.Sql.EodPrices.Upsert, eodPriceDao);

        string sql = $"{_context.Sql.EodPrices.Fetch} WHERE process_id = @ProcessId";

        var actual = _context.QueryFirstOrDefault<EodPrice>(
            sql, new { eodPriceDao.ProcessId });

        Assert.Equal(eodPriceDao, actual);
    }

    [Fact]
    public void InsertAndFetch_AdjustedEodPrices()
    {
        Guid processId = Guid.NewGuid();
        var eodPriceDao = CreateAdjustedEodPricesDao(processId);

        string sqlDelete = $@"{_context!.Sql.AdjustedEodPrices.Delete} WHERE
source = @Source AND code = @Code AND date_eod = @DateEod";

        _context.Execute(sqlDelete, eodPriceDao);
        _context.Execute(_context.Sql.AdjustedEodPrices.Upsert, eodPriceDao);

        string sql = $"{_context.Sql.AdjustedEodPrices.Fetch} WHERE process_id = @ProcessId";

        var actual = _context.QueryFirstOrDefault<AdjustedEodPrice>(
            sql, new { eodPriceDao.ProcessId });

        Assert.Equal(eodPriceDao, actual);
    }

    [Fact]
    public void InsertAndFetch_Splits()
    {
        Guid processId = Guid.NewGuid();
        var splitDao = CreateSplitDao(processId);

        _context!.Execute(_context.Sql.Splits.DeleteForSource, splitDao);
        _context.Execute(_context.Sql.Splits.Upsert, splitDao);

        string sql = $"{_context.Sql.Splits.Fetch} WHERE process_id = @ProcessId";

        var actual = _context.QueryFirstOrDefault<Split>(sql, new { splitDao.ProcessId });
        Assert.NotNull(actual);
        Assert.Equal(splitDao, actual);
    }

    [Fact]
    public void InsertAndFetch_Dividends()
    {
        Guid processId = Guid.NewGuid();
        var dividendDao = CreateDividendDao(processId);

        _context!.Execute(_context.Sql.Dividends.DeleteForSource, dividendDao);
        _context.Execute(_context.Sql.Dividends.Upsert, dividendDao);

        string sql = $"{_context.Sql.Dividends.Fetch} WHERE process_id = @ProcessId";

        var actual = _context.QueryFirstOrDefault<Dividend>(sql, new { dividendDao.ProcessId });
        Assert.NotNull(actual);
        Assert.Equal(dividendDao, actual);
    }

    [Fact]
    public void InsertAndFetch_BasicEntity()
    {
        var entity = new Entity("BTest", "BTEST.US");

        _context!.Execute(_context.Sql.Fundamentals.DeleteEntityForSourceAndCode,
            new { entity.Source, entity.Code });

        _context.Execute(_context.Sql.Fundamentals.InsertBasicEntity, entity);

        var entities = _context.Query<Entity>(_context.Sql.Fundamentals.FetchEntity);

        Assert.NotNull(entities);
        Assert.NotEmpty(entities);
        Assert.Contains(entity, entities);
    }

    [Fact]
    public void InsertAndFetch_Entities()
    {
        var entity = CreateEntity();

        _context!.Execute(_context.Sql.Fundamentals.DeleteEntityForSourceAndCode,
            new { entity.Source, entity.Code });

        _context.Execute(_context.Sql.Fundamentals.UpsertEntity, entity);

        var entities = _context.Query<Entity>(_context.Sql.Fundamentals.FetchEntity);

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
            DateEod = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedTicksUtc = DateTime.UtcNow.Ticks,
            UpdatedTicksUtc = DateTime.UtcNow.Ticks,
        };
    }

    private static AdjustedEodPrice CreateAdjustedEodPricesDao(Guid? processId = null)
    {
        var (Open, High, Low, Close, Volume) = StockPriceGenerator.GenerateRandomStockPrice(50M, 200M);

        return new AdjustedEodPrice("TEST", "TEST.US", processId ?? Guid.NewGuid())
        {
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume,
            DateEod = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedTicksUtc = DateTime.UtcNow.Ticks,
            UpdatedTicksUtc = DateTime.UtcNow.Ticks,
        };
    }

    private static Split CreateSplitDao(Guid? processId = null)
    {
        return new Split("TEST", "TEST.US", processId ?? Guid.NewGuid())
        {
            After = 1,
            Before = 2,
            SplitDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedTicksUtc = DateTime.UtcNow.Ticks,
            UpdatedTicksUtc = DateTime.UtcNow.Ticks,
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
            Frequency = 4,
            CreatedTicksUtc = DateTime.UtcNow.Ticks,
            UpdatedTicksUtc = DateTime.UtcNow.Ticks,
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
