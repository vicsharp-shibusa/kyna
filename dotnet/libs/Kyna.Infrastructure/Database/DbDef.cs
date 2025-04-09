using Kyna.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using System.Data;

namespace Kyna.Infrastructure.Database;

public sealed class DbDef
{
    public const string DefaultParmPrefix = "@";
    public const int MaxConnections = 50;

    public string Name;
    public DatabaseEngine Engine;
    public string ConnectionString;

    internal SqlCollection Sql { get; }

    private readonly SemaphoreSlim _connectionSemaphore;

    public DbDef(string name, DatabaseEngine engine, string connectionString, int maxConnections = MaxConnections)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(connectionString);
        if (engine == DatabaseEngine.None)
            throw new ArgumentException($"{nameof(engine)} cannot be {engine.GetEnumDescription()}");

        if (maxConnections < 1 || maxConnections > MaxConnections)
            throw new ArgumentOutOfRangeException($"{nameof(maxConnections)} must be between 1 and {maxConnections}");

        Name = name;
        Engine = engine;
        ConnectionString = connectionString;

        if (maxConnections < 1)
            throw new ArgumentOutOfRangeException(nameof(maxConnections), "Must be positive.");

        _connectionSemaphore = new SemaphoreSlim(maxConnections, maxConnections);

        Sql = new SqlCollection(SqlRepository.BuildDictionary(Engine));
    }

    public string ParameterPrefix => DefaultParmPrefix;

    public IDbConnection GetConnection()
    {
        _connectionSemaphore.Wait();
        try
        {
            IDbConnection conn = Engine switch
            {
                DatabaseEngine.PostgreSql => new NpgsqlConnection(ConnectionString),
                DatabaseEngine.MsSqlServer => new SqlConnection(ConnectionString),
                DatabaseEngine.MySql or DatabaseEngine.MariaDb => new MySqlConnection(ConnectionString),
                DatabaseEngine.Sqlite => new SqliteConnection(ConnectionString),
                _ => throw new ArgumentException("Invalid or unsupported database engine.")
            };

            return new PooledConnection(conn, ReleaseConnection);
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }

    private void ReleaseConnection()
    {
        _connectionSemaphore.Release();
    }

    private sealed class PooledConnection : IDbConnection
    {
        private readonly IDbConnection _innerConnection;
        private readonly Action _releaseAction;

        public PooledConnection(IDbConnection innerConnection, Action releaseAction)
        {
            ArgumentNullException.ThrowIfNull(innerConnection);
            ArgumentNullException.ThrowIfNull(releaseAction);

            _innerConnection = innerConnection;
            _releaseAction = releaseAction;
        }

        public string ConnectionString
        {
            get => _innerConnection.ConnectionString;
            set => _innerConnection.ConnectionString = value;
        }

        public int ConnectionTimeout => _innerConnection.ConnectionTimeout;

        public string Database => _innerConnection.Database;

        public ConnectionState State => _innerConnection.State;

        public IDbTransaction BeginTransaction() => _innerConnection.BeginTransaction();

        public IDbTransaction BeginTransaction(IsolationLevel il) => _innerConnection.BeginTransaction(il);

        public void ChangeDatabase(string databaseName) => _innerConnection.ChangeDatabase(databaseName);

        public void Close() => _innerConnection.Close();

        public IDbCommand CreateCommand() => _innerConnection.CreateCommand();

        public void Open() => _innerConnection.Open();

        public void Dispose()
        {
            _innerConnection.Dispose();
            _releaseAction();
        }
    }
}
