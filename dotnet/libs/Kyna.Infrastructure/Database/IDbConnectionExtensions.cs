using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Kyna.Infrastructure.Database;

/// <summary>
/// Contains a set of extensions for IDbConnection.
/// </summary>
public static class IDbConnectionExtensions
{
    private const int DefaultTimeoutSeconds = 5;

    /// <summary>
    /// Opens the connection if necessary and possible.
    /// </summary>
    /// <param name="connection">The <see cref="IDbConnection"> to open.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection state is broken or
    /// and is in an unknown state.</exception>
    public static void EnsureOpenConnection(this IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        switch (connection.State)
        {
            case ConnectionState.Open:
                break;
            case ConnectionState.Closed:
                connection.Open();
                break;
            case ConnectionState.Broken:
                throw new InvalidOperationException("The connection is in a broken state and cannot be used. Consider creating a new connection.");
            default:
                throw new InvalidOperationException($"Unexpected connection state: {connection.State}");
        }
    }

    /// <summary>
    /// Opens the connection if necessary and possible.
    /// </summary>
    /// <param name="connection">The <see cref="IDbConnection"> to open.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the underlying work.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection state is broken or
    /// and is in an unknown state.</exception>
    public static async Task EnsureOpenConnectionAsync(this IDbConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        cancellationToken.ThrowIfCancellationRequested();

        switch (connection.State)
        {
            case ConnectionState.Open:
                break;
            case ConnectionState.Closed:
                if (connection is DbConnection dbConn)
                    await dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
                else
                    connection.Open(); // fall back (IDbConnection doesn't have an Async method).
                break;
            case ConnectionState.Broken:
                throw new InvalidOperationException("The connection is in a broken state and cannot be used. Consider creating a new connection.");
            default:
                throw new InvalidOperationException($"Unexpected connection state: {connection.State}");
        }
    }

    /// <summary>
    /// Query the database.
    /// </summary>
    /// <typeparam name="T">The type of object returned.</typeparam>
    /// <param name="connection">The <see cref="IDbConnection"/>.</param>
    /// <param name="sql">The SQL statement to use for the query.</param>
    /// <param name="param">The object to which <paramref name="sql"/> maps.</param>
    /// <param name="transaction">The optional <see cref="IDbTransaction"/>.</param>
    /// <param name="parameterPrefix">The parameter prefix.</param>
    /// <returns></returns>
    public static IEnumerable<T> Query<T>(this IDbConnection connection, string? sql,
        object? param = null, int commandTimeout = DefaultTimeoutSeconds,
        IDbTransaction? transaction = null, string? parameterPrefix = DbDef.DefaultParmPrefix)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);

        EnsureOpenConnection(connection);

        using var reader = command.ExecuteReader();
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null))
        {
            while (reader.Read())
            {
                var value = reader.GetValue(0);
                if (!reader.IsDBNull(0))
                    yield return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T))
                        ?? typeof(T));
            }
        }
        else
        {
            var mapper = CreateMapper<T>(reader); // Hoisted outside the loop
            while (reader.Read())
            {
                var item = Activator.CreateInstance<T>();
                mapper(item, reader);
                yield return item;
            }
        }
    }

    /// <summary>
    /// Executes a query asynchronously and returns the results as a materialized enumerable.
    /// </summary>
    /// <typeparam name="T">The type to map the results to.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="param">The parameters for the query.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="parameterPrefix">The prefix for parameter names (e.g., '@' for SQL Server, ':' for PostgreSQL).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that resolves to an enumerable of results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sql"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the connection is in an unsupported state.</exception>
    /// <remarks>
    /// If the connection is closed, it will be opened automatically. The caller is responsible for disposing the connection.
    /// </remarks>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection,
        string? sql, object? param = null, int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
        string? parameterPrefix = DbDef.DefaultParmPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);
        cancellationToken.ThrowIfCancellationRequested();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);
        await EnsureOpenConnectionAsync(connection, cancellationToken);

        using var reader = command is DbCommand dbCmd
            ? await dbCmd.ExecuteReaderAsync(cancellationToken)
            : command.ExecuteReader();

        var results = new List<T>();
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null))
        {
            while (reader is DbDataReader dbReader
                ? await dbReader.ReadAsync(cancellationToken)
                : reader.Read())
            {
                var value = reader.GetValue(0);
                if (!reader.IsDBNull(0))
                {
                    results.Add((T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)));
                }
            }
        }
        else
        {
            var mapper = CreateMapper<T>(reader);
            while (reader is DbDataReader dbReader
                ? await dbReader.ReadAsync(cancellationToken)
                : reader.Read())
            {
                var item = Activator.CreateInstance<T>();
                mapper(item, reader);
                results.Add(item);
            }
        }
        return results;
    }

    public static async Task<IAsyncEnumerable<T>> QueryAsyncEnumerable<T>(this IDbConnection connection,
        string? sql, object? param = null, int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
        bool buffered = true, string? parameterPrefix = DbDef.DefaultParmPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);
        cancellationToken.ThrowIfCancellationRequested();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);
        await EnsureOpenConnectionAsync(connection, cancellationToken);

        using var reader = command is DbCommand dbCmd
            ? await dbCmd.ExecuteReaderAsync(cancellationToken)
            : command.ExecuteReader();

        if (buffered)
        {
            var results = new List<T>();
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
                || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null))
            {
                while (reader is DbDataReader dbReader
                    ? await dbReader.ReadAsync(cancellationToken)
                    : reader.Read())
                {
                    var value = reader.GetValue(0);
                    if (!reader.IsDBNull(0))
                    {
                        results.Add((T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)));
                    }
                }
            }
            else
            {
                var mapper = CreateMapper<T>(reader);
                while (reader is DbDataReader dbReader
                    ? await dbReader.ReadAsync(cancellationToken)
                    : reader.Read())
                {
                    var item = Activator.CreateInstance<T>();
                    mapper(item, reader);
                    results.Add(item);
                }
            }
            return results.ToAsyncEnumerable();
        }
        else
        {
            return QueryUnbufferedAsync<T>(reader, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<T> QueryUnbufferedAsync<T>(IDataReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null))
        {
            while (reader is DbDataReader dbReader
                ? await dbReader.ReadAsync(cancellationToken)
                : reader.Read())
            {
                var value = reader.GetValue(0);
                if (!reader.IsDBNull(0))
                {
                    yield return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
            }
        }
        else
        {
            var mapper = CreateMapper<T>(reader);
            while (reader is DbDataReader dbReader
                ? await dbReader.ReadAsync(cancellationToken)
                : reader.Read())
            {
                var item = Activator.CreateInstance<T>();
                mapper(item, reader);
                yield return item;
            }
        }
    }

    public static T? QueryFirstOrDefault<T>(this IDbConnection connection, string? sql,
        object? param = null, int commandTimeout = DefaultTimeoutSeconds,
        IDbTransaction? transaction = null, string? parameterPrefix = DbDef.DefaultParmPrefix)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);
        EnsureOpenConnection(connection);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return default;

        return typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null)
            ? MapScalar<T>(reader, 0)
            : MapObject<T>(reader);
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(this IDbConnection connection, string? sql,
        object? param = null, int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
        string? parameterPrefix = DbDef.DefaultParmPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);
        cancellationToken.ThrowIfCancellationRequested();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);
        await EnsureOpenConnectionAsync(connection, cancellationToken);

        using var reader = command is DbCommand dbCmd ? await dbCmd.ExecuteReaderAsync(cancellationToken) : command.ExecuteReader();
        if (!(reader is DbDataReader dbReader ? await dbReader.ReadAsync(cancellationToken) : reader.Read()))
            return default;

        return typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null)
            ? MapScalar<T>(reader, 0)
            : MapObject<T>(reader);
    }

    public static T? QuerySingleOrDefault<T>(this IDbConnection connection, string? sql,
        object? param = null, int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null, string? parameterPrefix = DbDef.DefaultParmPrefix)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);
        EnsureOpenConnection(connection);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return default;

        T? result = typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null)
            ? MapScalar<T>(reader, 0)
            : MapObject<T>(reader);

        if (reader.Read())
            throw new InvalidOperationException("Sequence contains more than one element.");
        return result;
    }

    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this IDbConnection connection, string? sql,
        object? param = null, int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
        string? parameterPrefix = DbDef.DefaultParmPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);
        cancellationToken.ThrowIfCancellationRequested();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeout;
        AddParameters(command, param, parameterPrefix);
        await EnsureOpenConnectionAsync(connection, cancellationToken);

        using var reader = command is DbCommand dbCmd
            ? await dbCmd.ExecuteReaderAsync(cancellationToken)
            : command.ExecuteReader();
        bool hasRow = reader is DbDataReader dbReader
            ? await dbReader.ReadAsync(cancellationToken)
            : reader.Read();
        if (!hasRow)
            return default;

        T? result = typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal)
            || (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) != null)
            ? MapScalar<T>(reader, 0)
            : MapObject<T>(reader);

        if (reader is DbDataReader dbRdr ? await dbRdr.ReadAsync(cancellationToken) : reader.Read())
            throw new InvalidOperationException("Sequence contains more than one element.");
        return result;
    }

    public static int Execute(this IDbConnection connection, string? sql, object? param = null,
        int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
        string? parameterPrefix = DbDef.DefaultParmPrefix)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);

        EnsureOpenConnection(connection);

        if (param is IEnumerable<object> collection && param is not string)
        {
            int totalRowsAffected = 0;
            foreach (var item in collection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandTimeout = commandTimeout;
                AddParameters(command, item, parameterPrefix);
                totalRowsAffected += command.ExecuteNonQuery();
            }
            return totalRowsAffected;
        }
        else
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.CommandTimeout = commandTimeout;
            AddParameters(command, param, parameterPrefix);
            return command.ExecuteNonQuery();
        }
    }
    //public static int Execute(this IDbConnection connection, string? sql, object? param = null,
    //    int commandTimeout = DefaultTimeoutSeconds,
    //    IDbTransaction? transaction = null, string? parameterPrefix = DbDef.DefaultParmPrefix)
    //{
    //    ArgumentNullException.ThrowIfNull(sql);
    //    if (commandTimeout < 0)
    //        throw new ArgumentOutOfRangeException(nameof(commandTimeout));

    //    using var command = connection.CreateCommand();
    //    command.CommandText = sql;
    //    command.Transaction = transaction;
    //    command.CommandTimeout = commandTimeout;
    //    AddParameters(command, param, parameterPrefix);
    //    EnsureOpenConnection(connection);
    //    return command.ExecuteNonQuery();
    //}

    public static async Task<int> ExecuteAsync(this IDbConnection connection, string? sql, object? param = null,
        int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
        string? parameterPrefix = DbDef.DefaultParmPrefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentOutOfRangeException.ThrowIfNegative(commandTimeout);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureOpenConnectionAsync(connection, cancellationToken).ConfigureAwait(false);

        if (param is IEnumerable<object> collection && param is not string)
        {
            int totalRowsAffected = 0;
            foreach (var item in collection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandTimeout = commandTimeout;
                AddParameters(command, item, parameterPrefix);
                totalRowsAffected += command is DbCommand dbCmd
                    ? await dbCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false)
                    : command.ExecuteNonQuery();
            }
            return totalRowsAffected;
        }
        else
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.CommandTimeout = commandTimeout;
            AddParameters(command, param, parameterPrefix);
            return command is DbCommand dbCmd
                ? await dbCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false)
                : command.ExecuteNonQuery();
        }
    }

    //public static async Task<int> ExecuteAsync(this IDbConnection connection, string? sql, object? param = null,
    //    int commandTimeout = DefaultTimeoutSeconds, IDbTransaction? transaction = null,
    //    string? parameterPrefix = DbDef.DefaultParmPrefix,
    //    CancellationToken cancellationToken = default)
    //{
    //    ArgumentNullException.ThrowIfNull(sql);
    //    if (commandTimeout < 0)
    //        throw new ArgumentOutOfRangeException(nameof(commandTimeout));
    //    cancellationToken.ThrowIfCancellationRequested();

    //    using var command = connection.CreateCommand();
    //    command.CommandText = sql;
    //    command.Transaction = transaction;
    //    command.CommandTimeout = commandTimeout;
    //    AddParameters(command, param, parameterPrefix);
    //    await EnsureOpenConnectionAsync(connection, cancellationToken);
    //    return command is DbCommand dbCmd
    //        ? await dbCmd.ExecuteNonQueryAsync(cancellationToken)
    //        : command.ExecuteNonQuery();
    //}

    private static void AddParameters(IDbCommand command, object? param, string? parameterPrefix = DbDef.DefaultParmPrefix)
    {
        if (param == null)
            return;

        foreach (var prop in param.GetType().GetProperties())
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"{parameterPrefix}{prop.Name}";
            var value = prop.GetValue(param);

            if (value is DateTimeOffset dto)
            {
                parameter.Value = dto.UtcDateTime;
            }
            else
            {
                parameter.Value = value ?? DBNull.Value;
            }

            command.Parameters.Add(parameter);
        }
    }

    private static readonly ConcurrentDictionary<Type, Delegate> _mapperCache = new();

    private static Action<T, IDataReader> CreateMapper<T>(IDataReader reader)
    {
        return (Action<T, IDataReader>)_mapperCache.GetOrAdd(typeof(T), type =>
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var columnNames = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var mappableProperties = properties
                .Where(p => columnNames.Contains(p.Key))
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

            return (T obj, IDataReader r) =>
            {
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var columnName = r.GetName(i);
                    if (mappableProperties.TryGetValue(columnName, out var prop) && !r.IsDBNull(i))
                    {
                        var value = r.GetValue(i);
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        try
                        {
                            if (targetType == typeof(DateTimeOffset) && value is DateTime dt)
                            {
                                // Convert UTC DateTime to local DateTimeOffset with millisecond precision
                                var localTime = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.Local);
                                var truncatedLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day,
                                    localTime.Hour, localTime.Minute, localTime.Second, localTime.Millisecond, localTime.Kind);
                                prop.SetValue(obj, new DateTimeOffset(truncatedLocalTime));
                            }
                            else if (targetType == typeof(DateOnly) && value is DateTime dateTime)
                            {
                                prop.SetValue(obj, DateOnly.FromDateTime(dateTime));
                            }
                            else if (targetType == typeof(decimal) && value is decimal dec)
                            {
                                prop.SetValue(obj, dec % 1 == 0 ? decimal.Truncate(dec) : dec);
                            }
                            //else if (targetType == typeof(long) && prop.Name.Equals("CreatedAtTicks", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    if (mappableProperties.TryGetValue("CreatedAt", out var createdAtProp))
                            //    {
                            //        var createdAt = (DateTimeOffset)createdAtProp.GetValue(obj)!;
                            //        prop.SetValue(obj, createdAt.Ticks);
                            //    }
                            //}
                            else
                            {
                                prop.SetValue(obj, Convert.ChangeType(value, targetType));
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Failed to map column '{columnName}' to property '{prop.Name}' of type '{targetType.Name}': {ex.Message}", ex);
                        }
                    }
                }
            };
        });
    }
    //private static Action<T, IDataReader> CreateMapper<T>(IDataReader reader)
    //{
    //    return (Action<T, IDataReader>)_mapperCache.GetOrAdd(typeof(T), type =>
    //    {
    //        var properties = typeof(T).GetProperties()
    //            .Where(p => p.CanWrite)
    //            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    //        var columnNames = Enumerable.Range(0, reader.FieldCount)
    //            .Select(i => reader.GetName(i))
    //            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    //        var mappableProperties = properties
    //            .Where(p => columnNames.Contains(p.Key))
    //            .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    //        return (T obj, IDataReader r) =>
    //        {
    //            for (int i = 0; i < r.FieldCount; i++)
    //            {
    //                var columnName = r.GetName(i);
    //                if (mappableProperties.TryGetValue(columnName, out var prop) && !r.IsDBNull(i))
    //                {
    //                    var value = r.GetValue(i);
    //                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

    //                    try
    //                    {
    //                        if (targetType == typeof(DateTimeOffset) && value is DateTime dt)
    //                        {
    //                            prop.SetValue(obj, new DateTimeOffset(dt));
    //                        }
    //                        else if (targetType == typeof(DateOnly) && value is DateTime dtdo)
    //                        {
    //                            // Convert DateTime to DateOnly
    //                            prop.SetValue(obj, DateOnly.FromDateTime(dtdo));
    //                        }
    //                        else
    //                        {
    //                            prop.SetValue(obj, Convert.ChangeType(value, targetType));
    //                        }
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        throw new InvalidOperationException(
    //                            $"Failed to map column '{columnName}' to property '{prop.Name}' of type '{targetType.Name}': {ex.Message}", ex);
    //                    }
    //                }
    //            }
    //        };
    //    });
    //}


    private static T? MapScalar<T>(IDataReader reader, int columnIndex)
    {
        var value = reader.GetValue(columnIndex);
        return reader.IsDBNull(columnIndex)
            ? default
            : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    private static T MapObject<T>(IDataReader reader)
    {
        var item = Activator.CreateInstance<T>();
        var mapper = CreateMapper<T>(reader);
        mapper(item, reader);
        return item;
    }
}