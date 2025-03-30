using Dapper;
using Npgsql;
using System.Data;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Kyna.Infrastructure.Tests"),
    InternalsVisibleTo("Kyna.ApplicationServices")]
namespace Kyna.Infrastructure.Database;

internal sealed class PostgreSqlContext(DbDef dbDef) : DbContextBase(dbDef), IDbContext
{
    public override IDbConnection GetOpenConnection()
    {
        var connection = GetConnection();
        connection.Open();
        return connection;
    }

    public override async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private NpgsqlConnection GetConnection()
    {
        // TODO: double-check this; postgresql might handle this stuff by now.
        var connection = new NpgsqlConnection(DbDef.ConnectionString);
        if (!SqlMapper.HasTypeHandler(typeof(SqlDateOnlyTypeHandler)))
        {
            SqlMapper.AddTypeHandler(new SqlDateOnlyTypeHandler());
        }
        if (!SqlMapper.HasTypeHandler(typeof(SqlTimeOnlyTypeHandler)))
        {
            SqlMapper.AddTypeHandler(new SqlTimeOnlyTypeHandler());
        }
        return connection;
    }

    public class SqlDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly date)
            => parameter.Value = date.ToDateTime(new TimeOnly(0, 0));

        public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);
    }

    public class SqlTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly time)
        {
            parameter.Value = time.ToString();
        }

        public override TimeOnly Parse(object value) => TimeOnly.FromTimeSpan((TimeSpan)value);
    }
}
