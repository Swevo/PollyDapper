# PollyDapper

[![NuGet](https://img.shields.io/nuget/v/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper)
[![CI](https://github.com/Swevo/PollyDapper/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollyDapper/actions)

**Polly v8 resilience pipelines for Dapper** — wrap `QueryAsync`, `ExecuteAsync`, and other Dapper calls with retry, timeout, circuit-breaker, and more using a single `ResilientDbConnection` decorator. Zero changes to your SQL.

```csharp
var resilient = connection.WithPolly(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        })
        .AddTimeout(TimeSpan.FromSeconds(5)));

var orders = await resilient.QueryAsync<Order>("SELECT * FROM Orders WHERE CustomerId = @Id", new { Id = id });
```

Every Dapper call is now automatically wrapped with retry + timeout — zero changes to existing SQL.

---

## Why PollyDapper?

Dapper is intentionally minimal — it gives you no interception point for cross-cutting concerns like retry or timeout. PollyDapper adds that layer cleanly.

| Without PollyDapper | With PollyDapper |
|---|---|
| Write try/catch + retry loops around every query | One `WithPolly(...)` call |
| Manually pass `CancellationToken` for timeouts | Timeout managed by the pipeline |
| Duplicate retry logic across repositories | Single pipeline applied everywhere |
| Must touch every query to add resilience | Zero changes to existing SQL |

---

## Installation

```bash
dotnet add package PollyDapper
```

Targets **net6.0**, **net8.0**, and **net9.0**.

Dependencies: `Polly.Core 8.*`, `Dapper 2.*`, `Microsoft.Extensions.DependencyInjection.Abstractions 8.*`

---

## Quick start

### 1. Inline pipeline (ad-hoc usage)

```csharp
using PollyDapper;

var resilient = connection.WithPolly(pipeline =>
    pipeline.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
    }));

var users = await resilient.QueryAsync<User>("SELECT * FROM Users");
```

### 2. Pre-built pipeline

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
    .AddTimeout(TimeSpan.FromSeconds(10))
    .Build();

var resilient = connection.WithPolly(pipeline);
var count = await resilient.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
```

### 3. Dependency injection

```csharp
// Program.cs
builder.Services.AddPollyDapper(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
        .AddTimeout(TimeSpan.FromSeconds(5)));

// Repository
public class OrderRepository(IDbConnection db, ResiliencePipeline pipeline)
{
    public Task<IEnumerable<Order>> GetAllAsync() =>
        db.WithPolly(pipeline).QueryAsync<Order>("SELECT * FROM Orders");

    public Task<int> InsertAsync(Order order) =>
        db.WithPolly(pipeline).ExecuteAsync(
            "INSERT INTO Orders (CustomerId, Total) VALUES (@CustomerId, @Total)", order);
}
```

---

## Supported methods

| Method | Description |
|--------|-------------|
| `QueryAsync<T>` | Returns `IEnumerable<T>` |
| `QueryFirstAsync<T>` | First row, throws if empty |
| `QueryFirstOrDefaultAsync<T>` | First row or `default` |
| `QuerySingleAsync<T>` | Exactly one row, throws otherwise |
| `QuerySingleOrDefaultAsync<T>` | One row or `default` |
| `ExecuteAsync` | Rows affected |
| `ExecuteScalarAsync<T>` | First column of first row |

---

## Pipeline order

Polly strategies are applied outer-to-inner (left-to-right). The recommended order is:

```
[Timeout] → [Retry] → [Circuit Breaker] → [Dapper]
```

```csharp
pipeline
    .AddTimeout(TimeSpan.FromSeconds(10))    // 1. Overall deadline
    .AddRetry(retryOptions)                  // 2. Retry on failure
    .AddCircuitBreaker(cbOptions)            // 3. Open circuit if overloaded
```

---

## ASP.NET Core example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IDbConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddPollyDapper(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(100),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder()
                .Handle<SqlException>(ex => ex.IsTransient),
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
        }));
```

---

## Related packages

| Package | Downloads | Description |
|---|---|---|
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience for EF Core queries and SaveChanges |
| [PollySqlClient](https://www.nuget.org/packages/PollySqlClient) | [![Downloads](https://img.shields.io/nuget/dt/PollySqlClient.svg)](https://www.nuget.org/packages/PollySqlClient) | Polly v8 resilience for SQL Server and Azure SQL with SqlServerTransientErrors predicate |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 resilience pipelines for MediatR |
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers |
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI — retry on 429, Retry-After, circuit breaker |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollySignalR](https://www.nuget.org/packages/PollySignalR) | [![Downloads](https://img.shields.io/nuget/dt/PollySignalR.svg)](https://www.nuget.org/packages/PollySignalR) | Polly v8 exponential back-off reconnect policy for SignalR |
| [PollyElasticsearch](https://github.com/Swevo/PollyElasticsearch) | Polly v8 for Elastic.Clients.Elasticsearch |
| [PollyAzureKeyVault](https://github.com/Swevo/PollyAzureKeyVault) | Polly v8 for Azure Key Vault |
| [PollyAzureEventHub](https://github.com/Swevo/PollyAzureEventHub) | Polly v8 for Azure Event Hubs |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Jitter, linear & custom backoff for Polly v8 retry |
| [PollyChaos](https://www.nuget.org/packages/PollyChaos) | [![Downloads](https://img.shields.io/nuget/dt/PollyChaos.svg)](https://www.nuget.org/packages/PollyChaos) | Fault & latency injection (Simmy for Polly v8) |

---

## License

MIT
