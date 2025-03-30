using Dapper;
using Kyna.Infrastructure.Logging;
using System.Data;
using System.Diagnostics;

namespace Kyna.Infrastructure.Database;

internal abstract class DbContextBase(DbDef dbDef)
{
    public DbDef DbDef { get; } = dbDef;

    public SqlRepository Sql { get; } = new(dbDef);

    public abstract IDbConnection GetOpenConnection();

    public abstract Task<IDbConnection> GetOpenConnectionAsync(
        CancellationToken cancellationToken = default);

    public void Execute(string sql, object? parameters = null, 
        IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

        bool isLocalTransaction = transaction is null;

        var connection = isLocalTransaction 
            ? GetOpenConnection() 
            : transaction!.Connection;

        Debug.Assert(connection is not null);
        Debug.Assert(connection.State == ConnectionState.Open);

        transaction ??= connection.BeginTransaction();

        try
        {
            connection.Execute(sql, parameters, transaction, commandTimeout);
            if (isLocalTransaction)
            {
                transaction.Commit();
            }
        }
        catch (Exception exc)
        {
            if (isLocalTransaction)
            {
                transaction.Rollback();
            }
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

    public async Task ExecuteAsync(string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

        bool isLocalTransaction = transaction is null;

        var connection = isLocalTransaction
            ? await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false)
            : transaction!.Connection;

        Debug.Assert(connection is not null);
        Debug.Assert(connection.State == ConnectionState.Open);

        transaction ??= connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, parameters, transaction, commandTimeout).ConfigureAwait(false);
            if (isLocalTransaction)
            {
                transaction.Commit();
            }
        }
        catch (Exception exc)
        {
            if (isLocalTransaction)
            {
                transaction.Rollback();
            }
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
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

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

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

        using var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout).ConfigureAwait(false);
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

    public T? QueryFirstOrDefault<T>(string sql,
        object? parameters = null,
        int? commandTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

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

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentNullException(nameof(sql));
        }

        using var connection = GetOpenConnection();

        try
        {
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandTimeout: commandTimeout).ConfigureAwait(false);
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
