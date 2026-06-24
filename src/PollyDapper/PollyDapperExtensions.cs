// <copyright file="PollyDapperExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyDapper;

/// <summary>
/// Extension methods for wrapping an <see cref="IDbConnection"/> with a Polly v8 resilience pipeline.
/// </summary>
public static class PollyDapperExtensions
{
    /// <summary>
    /// Wraps <paramref name="connection"/> in a <see cref="ResilientDbConnection"/> that executes
    /// every Dapper call inside the supplied <paramref name="pipeline"/>.
    /// </summary>
    /// <param name="connection">The underlying database connection.</param>
    /// <param name="pipeline">The Polly v8 resilience pipeline to apply to every query and command.</param>
    /// <returns>A <see cref="ResilientDbConnection"/> backed by <paramref name="connection"/>.</returns>
    public static ResilientDbConnection WithPolly(
        this IDbConnection connection,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return new ResilientDbConnection(connection, pipeline);
    }
}
