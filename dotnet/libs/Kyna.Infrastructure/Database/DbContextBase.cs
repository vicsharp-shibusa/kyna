using Dapper;
using Kyna.Common.Logging;
using System.Data;
using System.Diagnostics;

namespace Kyna.Infrastructure.Database;

internal abstract class DbContextBase
{
    public DbContextBase(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public abstract IDbConnection GetOpenConnection();

    public abstract Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    public void Execute(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) { throw new ArgumentNullException(nameof(sql)); }

        bool isLocalTransaction = transaction is null;

        var connection = isLocalTransaction ? GetOpenConnection() : transaction!.Connection;

        Debug.Assert(connection is not null);
        Debug.Assert(connection.State == ConnectionState.Open);

        transaction ??= connection.BeginTransaction();

        try
        {
            connection.Execute(sql, parameters, transaction, commandTimeout);
            if (isLocalTransaction) { transaction.Commit(); }
        }
        catch (Exception exc)
        {
            if (isLocalTransaction) { transaction.Rollback(); }
            KLogger.LogCritical(exc);
            throw;
        }
        finally
        {
            if (isLocalTransaction)
            {
                connection.Close();
            }
        }
    }

    public async Task ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql)) { throw new ArgumentNullException(nameof(sql)); }

        bool isLocalTransaction = transaction is null;

        var connection = isLocalTransaction ? await GetOpenConnectionAsync(cancellationToken) : transaction!.Connection;

        Debug.Assert(connection is not null);
        Debug.Assert(connection.State == ConnectionState.Open);

        transaction ??= connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, parameters, transaction, commandTimeout);
            if (isLocalTransaction) { transaction.Commit(); }
        }
        catch (Exception exc)
        {
            if (isLocalTransaction) { transaction.Rollback(); }
            KLogger.LogCritical(exc);
            throw;
        }
        finally
        {
            if (isLocalTransaction)
            {
                connection.Close();
            }
        }
    }

    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) { throw new ArgumentNullException(nameof(sql)); }

        using var connection = GetOpenConnection();

        try
        {
            return connection.Query<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        catch (Exception exc)
        {
            KLogger.LogCritical(exc);
            throw;
        }
        finally
        {
            connection.Close();
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql)) { throw new ArgumentNullException(nameof(sql)); }

        using var connection = await GetOpenConnectionAsync(cancellationToken);

        try
        {
            return await connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        catch (Exception exc)
        {
            KLogger.LogCritical(exc);
            throw;
        }
        finally
        {
            connection.Close();
        }
    }

    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) { throw new ArgumentNullException(nameof(sql)); }

        using var connection = GetOpenConnection();

        try
        {
            return connection.QueryFirstOrDefault<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        catch (Exception exc)
        {
            KLogger.LogCritical(exc);
            throw;
        }
        finally
        {
            connection.Close();
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql)) { throw new ArgumentNullException(nameof(sql)); }

        using var connection = GetOpenConnection();

        try
        {
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        catch (Exception exc)
        {
            KLogger.LogCritical(exc);
            throw;
        }
        finally
        {
            connection.Close();
        }
    }
}
