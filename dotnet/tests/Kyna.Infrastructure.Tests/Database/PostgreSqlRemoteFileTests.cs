using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kyna.Infrastructure.Tests.Database;

public class PostgreSqlRemoteFileTests
{
    private PostgreSqlContext? _context;
    private const string DbName = "Imports";

    public PostgreSqlRemoteFileTests()
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
    public void InsertAndFetch_RemoteFile_InternalTransaction()
    {
        var remoteFileDao = CreateRemoteFile(Guid.NewGuid());

        _context!.Execute(_context.Sql.RemoteFiles.DeleteForSource, new { Source = "Test" });

        _context.Execute(_context.Sql.RemoteFiles.Upsert, remoteFileDao);

        string sql = $"{_context.Sql.RemoteFiles.Fetch} WHERE process_id = @ProcessId";

        var actual = _context.QueryFirstOrDefault<RemoteFile>(sql, new { remoteFileDao.ProcessId });

        Assert.Equal(remoteFileDao, actual);
    }

    private static RemoteFile CreateRemoteFile(Guid? processId = null) =>
        new RemoteFile()
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
