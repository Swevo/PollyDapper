// <copyright file="ResilientDbConnection.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyDapper;

/// <summary>
/// A Dapper decorator that executes every query and command inside a Polly v8
/// <see cref="ResiliencePipeline"/>. Create one via
/// <see cref="PollyDapperExtensions.WithPolly(IDbConnection, ResiliencePipeline)"/>.
/// </summary>
public sealed class ResilientDbConnection(IDbConnection inner, ResiliencePipeline pipeline)
{
    /// <summary>
    /// Executes a query, returning the data typed as <typeparamref name="T"/>.
    /// </summary>
    public Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<IEnumerable<T>>(
                inner.QueryAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a query, returning the first result typed as <typeparamref name="T"/>.
    /// </summary>
    public Task<T> QueryFirstAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<T>(
                inner.QueryFirstAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a query, returning the first result or <c>default</c> if the sequence is empty.
    /// </summary>
    public Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<T?>(
                inner.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a query, returning exactly one result typed as <typeparamref name="T"/>.
    /// </summary>
    public Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<T>(
                inner.QuerySingleAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a query, returning exactly one result or <c>default</c> if the sequence is empty.
    /// </summary>
    public Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<T?>(
                inner.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a command and returns the number of rows affected.
    /// </summary>
    public Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<int>(
                inner.ExecuteAsync(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a command and returns the first column of the first row as <typeparamref name="T"/>.
    /// </summary>
    public Task<T> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<T>(
                inner.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: ct))!),
            cancellationToken).AsTask();
}
