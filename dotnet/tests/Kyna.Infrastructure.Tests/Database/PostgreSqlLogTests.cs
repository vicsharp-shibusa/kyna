using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlLogTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlLogTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_Log_InternalTransaction()
    {
        var logDao = CreateLog();

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertLog), logDao);

        var sql = _fixture.Logs.Sql.GetSql(SqlKeys.FetchLogs, "message = @Message");

        var actual = context.QueryFirstOrDefault<Log>(sql,
            new { logDao.Message });

        Assert.NotNull(actual);
        Assert.Equal(logDao, actual);
    }

    [Fact]
    public async Task InsertAndFetch_Log_InternalTransactionAsync()
    {
        var logDao = CreateLog();

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        await context!.ExecuteAsync(_fixture.Logs.Sql.GetSql(SqlKeys.InsertLog), logDao);

        var sql = _fixture.Logs.Sql.GetSql(SqlKeys.FetchLogs, "message = @Message");

        var actual = await context.QueryFirstOrDefaultAsync<Log>(sql, new { logDao.Message });

        Assert.NotNull(actual);
        Assert.Equal(logDao, actual);
    }

    [Fact]
    public void InsertAndFetch_Log_ExternalTransaction()
    {
        var logDao1 = CreateLog();
        var logDao2 = CreateLog();

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        context.EnsureOpenConnection();
        var t = context.BeginTransaction();

        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertLog), logDao1, transaction: t);
        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertLog), logDao2, transaction: t);

        t.Commit();
        t.Connection?.Close();

        var actuals = context.Query<Log>(
            _fixture.Logs.Sql.GetSql(SqlKeys.FetchLogs));

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

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        context.EnsureOpenConnection();
        using var t = context.BeginTransaction();

        await context.ExecuteAsync(_fixture.Logs.Sql.GetSql(SqlKeys.InsertLog), logDao1, transaction: t);
        await context.ExecuteAsync(_fixture.Logs.Sql.GetSql(SqlKeys.InsertLog), logDao2, transaction: t);

        t.Commit();
        t.Connection?.Close();

        var actuals = await context.QueryAsync<Log>(
            _fixture.Logs.Sql.GetSql(SqlKeys.FetchLogs));

        var match1 = actuals.FirstOrDefault(a => a.ProcessId.Equals(logDao1.ProcessId));
        var match2 = actuals.FirstOrDefault(a => a.ProcessId.Equals(logDao2.ProcessId));

        Assert.Equal(logDao1, match1);
        Assert.Equal(logDao2, match2);
    }

    [Fact]
    public void InsertAndFetch_AppEvent_InternalTransaction()
    {
        var eventDao = CreateAppEvent();

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertAppEvent), eventDao);

        var sql = _fixture.Logs.Sql.GetSql(SqlKeys.FetchAppEvents, "event_name = @EventName");

        var actual = context.QueryFirstOrDefault<AppEvent>(sql,
            new { eventDao.EventName });

        Assert.NotNull(actual);
        Assert.Equal(eventDao, actual);
    }

    [Fact]
    public async Task InsertAndFetch_AppEvent_InternalTransactionAsync()
    {
        var eventDao = CreateAppEvent();

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        await context!.ExecuteAsync(_fixture.Logs.Sql.GetSql(SqlKeys.InsertAppEvent), eventDao);

        var sql = _fixture.Logs.Sql.GetSql(SqlKeys.FetchAppEvents, "event_name = @EventName");

        var actual = await context.QueryFirstOrDefaultAsync<AppEvent>(sql,
            new { eventDao.EventName });

        Assert.NotNull(actual);
        Assert.Equal(eventDao, actual);
    }

    [Fact]
    public void InsertAndFetch_AppEvent_ExternalTransaction()
    {
        var eventDao1 = CreateAppEvent();
        var eventDao2 = CreateAppEvent();

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        context.EnsureOpenConnection();

        context.EnsureOpenConnection();
        var t = context.BeginTransaction();

        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertAppEvent), eventDao1, transaction: t);
        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertAppEvent), eventDao2, transaction: t);

        t.Commit();
        t.Connection?.Close();

        var actuals = context.Query<AppEvent>(
            _fixture.Logs.Sql.GetSql(SqlKeys.FetchAppEvents), new { eventDao1.EventName });

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

        using var context = _fixture.Logs.GetConnection();
        Assert.NotNull(context);

        context.EnsureOpenConnection();

        context.EnsureOpenConnection();
        var t = context.BeginTransaction();

        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertAppEvent), eventDao1, transaction: t);
        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.InsertAppEvent), eventDao2, transaction: t);

        t.Commit();
        t.Connection?.Close();

        var actuals = await context.QueryAsync<AppEvent>(
            _fixture.Logs.Sql.GetSql(SqlKeys.FetchAppEvents), new { eventDao1.EventName });

        var match1 = actuals.FirstOrDefault(a => a.ProcessId.Equals(eventDao1.ProcessId));
        var match2 = actuals.FirstOrDefault(a => a.ProcessId.Equals(eventDao2.ProcessId));

        Assert.Equal(eventDao1, match1);
        Assert.Equal(eventDao2, match2);
    }

    private static Log CreateLog()
    {
        return new Log()
        {
            Exception = "exception",
            LogLevel = "Debug",
            Message = Guid.NewGuid().ToString("N"),
            ProcessId = Guid.NewGuid(),
            Scope = "scope"
        };
    }

    private static AppEvent CreateAppEvent()
    {
        return new AppEvent()
        {
            ProcessId = Guid.NewGuid(),
            EventId = 1010,
            EventName = Guid.NewGuid().ToString("N"),
        };
    }
}
