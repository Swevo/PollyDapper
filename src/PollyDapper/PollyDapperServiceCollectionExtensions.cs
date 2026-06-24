// <copyright file="PollyDapperServiceCollectionExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyDapper;

/// <summary>
/// Extension methods for registering a shared <see cref="ResiliencePipeline"/> with the
/// Microsoft dependency-injection container for use with Dapper.
/// </summary>
public static class PollyDapperServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ResiliencePipeline"/> singleton built from <paramref name="configure"/>,
    /// which can then be injected alongside <see cref="IDbConnection"/> and used via
    /// <see cref="PollyDapperExtensions.WithPolly"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// A delegate that configures the <see cref="ResiliencePipelineBuilder"/>
    /// (e.g. adds retry, timeout, circuit-breaker strategies).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPollyDapper(
        this IServiceCollection services,
        Action<ResiliencePipelineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ResiliencePipelineBuilder();
        configure(builder);
        return services.AddPollyDapper(builder.Build());
    }

    /// <summary>
    /// Registers a pre-built <see cref="ResiliencePipeline"/> singleton that can be injected
    /// alongside <see cref="IDbConnection"/> and used via
    /// <see cref="PollyDapperExtensions.WithPolly"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pipeline">A fully configured <see cref="ResiliencePipeline"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPollyDapper(
        this IServiceCollection services,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pipeline);

        services.AddSingleton(pipeline);
        return services;
    }
}
