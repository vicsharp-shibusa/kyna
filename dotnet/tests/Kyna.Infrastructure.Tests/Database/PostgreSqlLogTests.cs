using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlLogTests
{
    private readonly SqlRepository _postgreSqlRepo = new(DatabaseEngine.PostgreSql);
    private const string DbName = "Logs";

    private PostgreSqlContext? _context;

    public PostgreSqlLogTests()
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
    public void InsertAndFetch_Log_InternalTransaction()
    {
        var logDao = CreateLog();

        _context!.Execute(_postgreSqlRepo.Logs.Insert, logDao);

        string sql = $"{_postgreSqlRepo.Logs.Fetch} WHERE message = @Message";

        var actual = _context.QueryFirstOrDefault<Infrastructure.Database.DataAccessObjects.Log>(sql, new { logDao.Message });

        Assert.NotNull(actual);
        Assert.Equal(logDao, actual);
    }

    [Fact]
    public async Task InsertAndFetch_Log_InternalTransactionAsync()
    {
        var logDao = CreateLog();

        await _context!.ExecuteAsync(_postgreSqlRepo.Logs.Insert, logDao);

        string sql = $"{_postgreSqlRepo.Logs.Fetch} WHERE message = @Message";

        var actual = await _context.QueryFirstOrDefaultAsync<Infrastructure.Database.DataAccessObjects.Log>(sql, new { logDao.Message });

        Assert.NotNull(actual);
        Assert.Equal(logDao, actual);
    }

    [Fact]
    public void InsertAndFetch_Log_ExternalTransaction()
    {
        var logDao1 = CreateLog();
        var logDao2 = CreateLog();

        var t = _context!.GetOpenConnection().BeginTransaction();

        _context.Execute(_postgreSqlRepo.Logs.Insert, logDao1, t);
        _context.Execute(_postgreSqlRepo.Logs.Insert, logDao2, t);

        t.Commit();
        t.Connection?.Close();

        var actuals = _context.Query<Infrastructure.Database.DataAccessObjects.Log>(
            _postgreSqlRepo.Logs.Fetch);

        var match1 = actuals.FirstOrDefault(a => a.ProcessId.Equals(logDao1.ProcessId));
        var match2 = actuals.FirstOrDefault(a => a.ProcessId.Equals(logDao2.ProcessId));

        Assert.Equal(logDao1, match1);
        Assert.Equal(logDao2, match2);
    }

    [Fact]
    public async Task InsertAndFetch_Log_ExternalTransactionAsync()
    {
        var logDao1 = CreateLog();
        var logDao2 = CreateLog();

        var t = (await _context!.GetOpenConnectionAsync()).BeginTransaction();

        await _context.ExecuteAsync(_postgreSqlRepo.Logs.Insert, logDao1, t);
        await _context.ExecuteAsync(_postgreSqlRepo.Logs.Insert, logDao2, t);

        t.Commit();
        t.Connection?.Close();

        var actuals = await _context.QueryAsync<Infrastructure.Database.DataAccessObjects.Log>(
            _postgreSqlRepo.Logs.Fetch);

        var match1 = actuals.FirstOrDefault(a => a.ProcessId.Equals(logDao1.ProcessId));
        var match2 = actuals.FirstOrDefault(a => a.ProcessId.Equals(logDao2.ProcessId));

        Assert.Equal(logDao1, match1);
        Assert.Equal(logDao2, match2);
    }

    [Fact]
    public void InsertAndFetch_AppEvent_InternalTransaction()
    {
        var eventDao = CreateAppEvent();

        _context!.Execute(_postgreSqlRepo.AppEvents.Insert, eventDao);

        string sql = $"{_postgreSqlRepo.AppEvents.Fetch} WHERE event_name = @EventName";

        var actual = _context.QueryFirstOrDefault<Infrastructure.Database.DataAccessObjects.AppEvent>(sql, new { eventDao.EventName });

        Assert.NotNull(actual);
        Assert.Equal(eventDao, actual);
    }

    [Fact]
    public async Task InsertAndFetch_AppEvent_InternalTransactionAsync()
    {
        var eventDao = CreateAppEvent();

        await _context!.ExecuteAsync(_postgreSqlRepo.AppEvents.Insert, eventDao);

        string sql = $"{_postgreSqlRepo.AppEvents.Fetch} WHERE event_name = @EventName";

        var actual = await _context.QueryFirstOrDefaultAsync<Infrastructure.Database.DataAccessObjects.AppEvent>(sql, new { eventDao.EventName });

        Assert.NotNull(actual);
        Assert.Equal(eventDao, actual);
    }

    [Fact]
    public void InsertAndFetch_AppEvent_ExternalTransaction()
    {
        var eventDao1 = CreateAppEvent();
        var eventDao2 = CreateAppEvent();

        var t = _context!.GetOpenConnection().BeginTransaction();

        _context.Execute(_postgreSqlRepo.AppEvents.Insert, eventDao1, t);
        _context.Execute(_postgreSqlRepo.AppEvents.Insert, eventDao2, t);

        t.Commit();
        t.Connection?.Close();

        var actuals = _context.Query<Infrastructure.Database.DataAccessObjects.AppEvent>(
            _postgreSqlRepo.AppEvents.Fetch, new { eventDao1.EventName });

        var match1 = actuals.FirstOrDefault(a => a.ProcessId.Equals(eventDao1.ProcessId));
        var match2 = actuals.FirstOrDefault(a => a.ProcessId.Equals(eventDao2.ProcessId));

        Assert.Equal(eventDao1, match1);
        Assert.Equal(eventDao2, match2);
    }

    [Fact]
    public async Task InsertAndFetch_AppEvent_ExternalTransactionAsync()
    {
        var eventDao1 = CreateAppEvent();
        var eventDao2 = CreateAppEvent();

        var t = (await _context!.GetOpenConnectionAsync()).BeginTransaction();

        await _context.ExecuteAsync(_postgreSqlRepo.AppEvents.Insert, eventDao1, t);
        await _context.ExecuteAsync(_postgreSqlRepo.AppEvents.Insert, eventDao2, t);

        t.Commit();
        t.Connection?.Close();

        var actuals = await _context.QueryAsync<Infrastructure.Database.DataAccessObjects.AppEvent>(
            _postgreSqlRepo.AppEvents.Fetch, new { eventDao1.EventName });

        var match1 = actuals.FirstOrDefault(a => a.ProcessId.Equals(eventDao1.ProcessId));
        var match2 = actuals.FirstOrDefault(a => a.ProcessId.Equals(eventDao2.ProcessId));

        Assert.Equal(eventDao1, match1);
        Assert.Equal(eventDao2, match2);
    }

    private static Infrastructure.Database.DataAccessObjects.Log CreateLog()
    {
        return new Infrastructure.Database.DataAccessObjects.Log()
        {
            Exception = "exception",
            LogLevel = "Debug",
            Message = Guid.NewGuid().ToString("N"),
            ProcessId = Guid.NewGuid(),
            Scope = "scope"
        };
    }

    private static Infrastructure.Database.DataAccessObjects.AppEvent CreateAppEvent()
    {
        return new Infrastructure.Database.DataAccessObjects.AppEvent()
        {
            ProcessId = Guid.NewGuid(),
            EventId = 1010,
            EventName = Guid.NewGuid().ToString("N"),
        };
    }
}
