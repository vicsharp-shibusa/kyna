using Npgsql;
using System.Data;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Kyna.Infrastructure.Tests"),
    InternalsVisibleTo("Kyna.ApplicationServices")]
namespace Kyna.Infrastructure.Database;

internal class PostgreSqlContext(DbDef dbDef) : DbContextBase(dbDef), IDbContext
{
    public override IDbConnection GetOpenConnection()
    {
        var connection = new NpgsqlConnection(DbDef.ConnectionString);
        connection.Open();
        return connection;
    }

    public override async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = new NpgsqlConnection(DbDef.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
