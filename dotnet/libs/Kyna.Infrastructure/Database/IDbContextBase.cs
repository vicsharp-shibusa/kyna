﻿using System.Data;

namespace Kyna.Infrastructure.Database;

internal interface IDbContext
{
    string ConnectionString { get; }

    void Execute(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null);

    Task ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

    IDbConnection GetOpenConnection();

    Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    IEnumerable<T> Query<T>(string sql, object? parameters = null, int? commandTimeout = null);

    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

    T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? commandTimeout = null);

    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
}