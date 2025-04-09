using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlRemoteFileTests : IClassFixture<PostgreSqlTestFixture>
{
    private readonly PostgreSqlTestFixture _fixture;

    public PostgreSqlRemoteFileTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertAndFetch_RemoteFile_InternalTransaction()
    {
        var remoteFileDao = CreateRemoteFile(Guid.NewGuid());

        using var context = _fixture.Imports.GetConnection();
        Assert.NotNull(context);

        context!.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.DeleteRemoteFilesForSource), new { Source = "Test" });

        context.Execute(_fixture.Logs.Sql.GetSql(SqlKeys.UpsertRemoteFile), remoteFileDao);

        var sql = _fixture.Logs.Sql.GetSql(SqlKeys.FetchRemoteFiles, "process_id = @ProcessId");

        var actual = context.QueryFirstOrDefault<RemoteFile>(sql, new { remoteFileDao.ProcessId });

        Assert.Equal(remoteFileDao, actual);
    }

    private static RemoteFile CreateRemoteFile(Guid? processId = null) =>
        new()
        {
            Source = "Test",
            Location = "FlatFiles",
            HashCode = Guid.NewGuid().ToString(),
            Name = "Test.csv",
            Provider = "AWS",
            Size = 1_000L,
            UpdateDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessId = processId ?? Guid.NewGuid()
        };
}
