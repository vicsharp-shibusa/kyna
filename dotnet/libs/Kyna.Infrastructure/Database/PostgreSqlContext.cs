using Npgsql;
using System.Data;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Kyna.Infrastructure.Tests")]
namespace Kyna.Infrastructure.Database;

internal class PostgreSqlContext(string? connectionString) : DbContextBase(connectionString), IDbContext
{
    public override IDbConnection GetOpenConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public override async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
