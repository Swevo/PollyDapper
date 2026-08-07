# PollyDapper

[![NuGet](https://img.shields.io/nuget/v/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper)
[![CI](https://github.com/Swevo/PollyDapper/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollyDapper/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10 Ready](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](#)

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

## Related Packages

| Package | Downloads | Description |
|---|---|---|
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers — expose circuit-breaker state (Closed, HalfOpen, Open, Isolated) as /health endpoint responses |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Backoff delay strategies for Polly v8 resilience pipelines |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience pipelines for Entity Framework Core — wrap every EF Core query and SaveChanges with retry, timeout and circuit-breaker via a single AddPollyResilience() call |
| [PollyMailKit](https://www.nuget.org/packages/PollyMailKit) | [![Downloads](https://img.shields.io/nuget/dt/PollyMailKit.svg)](https://www.nuget.org/packages/PollyMailKit) | Polly v8 resilience pipelines for MailKit — retry, timeout, and circuit-breaker for SmtpClient.SendAsync and any MailKit SMTP operation |
| [PollyMassTransit](https://www.nuget.org/packages/PollyMassTransit) | [![Downloads](https://img.shields.io/nuget/dt/PollyMassTransit.svg)](https://www.nuget.org/packages/PollyMassTransit) | Polly v8 resilience pipelines for MassTransit — retry, timeout, and circuit-breaker for IBus.Publish and ISendEndpointProvider.Send |
| [PollyAzureEventHub](https://www.nuget.org/packages/PollyAzureEventHub) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureEventHub.svg)](https://www.nuget.org/packages/PollyAzureEventHub) | Polly v8 resilience pipelines for Azure Event Hubs — retry, timeout, and circuit-breaker for EventHubProducerClient and EventHubConsumerClient |
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI API calls |
| [PollySignalR](https://www.nuget.org/packages/PollySignalR) | [![Downloads](https://img.shields.io/nuget/dt/PollySignalR.svg)](https://www.nuget.org/packages/PollySignalR) | Polly v8 reconnect policy for SignalR |
| [PollyElasticsearch](https://www.nuget.org/packages/PollyElasticsearch) | [![Downloads](https://img.shields.io/nuget/dt/PollyElasticsearch.svg)](https://www.nuget.org/packages/PollyElasticsearch) | Polly v8 resilience pipelines for Elastic.Clients.Elasticsearch 8+ — retry, timeout, and circuit-breaker for any Elasticsearch operation, plus a built-in ElasticTransientErrors predicate covering rate limiting (429), service unavailability (503), gateway timeouts (504), and connection failures |
| [PollyHangfire](https://www.nuget.org/packages/PollyHangfire) | [![Downloads](https://img.shields.io/nuget/dt/PollyHangfire.svg)](https://www.nuget.org/packages/PollyHangfire) | Polly v8 resilience pipelines for Hangfire — retry, timeout, and circuit-breaker for IBackgroundJobClient.Enqueue and Schedule |
| [PollySendGrid](https://www.nuget.org/packages/PollySendGrid) | [![Downloads](https://img.shields.io/nuget/dt/PollySendGrid.svg)](https://www.nuget.org/packages/PollySendGrid) | Polly v8 resilience pipelines for SendGrid — retry, timeout, and circuit-breaker for ISendGridClient.SendEmailAsync |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 resilience pipelines for MediatR — add retry, timeout, circuit-breaker, rate-limiting, hedging, and chaos engineering to any MediatR request handler with a single line of DI registration |
| [PollyAzureKeyVault](https://www.nuget.org/packages/PollyAzureKeyVault) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureKeyVault.svg)](https://www.nuget.org/packages/PollyAzureKeyVault) | Polly v8 resilience pipelines for Azure Key Vault — retry, timeout, and circuit-breaker for SecretClient, KeyClient, and CertificateClient |
| [PollyAzureQueueStorage](https://www.nuget.org/packages/PollyAzureQueueStorage) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureQueueStorage.svg)](https://www.nuget.org/packages/PollyAzureQueueStorage) | Polly v8 resilience pipelines for Azure Queue Storage — retry, timeout, and circuit-breaker for Azure.Storage.Queues QueueClient |
| [PollySqlClient](https://www.nuget.org/packages/PollySqlClient) | [![Downloads](https://img.shields.io/nuget/dt/PollySqlClient.svg)](https://www.nuget.org/packages/PollySqlClient) | Polly v8 resilience pipelines for Microsoft.Data.SqlClient (SQL Server and Azure SQL) — retry, timeout, and circuit-breaker for SqlConnection queries and commands, plus a built-in SqlServerTransientErrors predicate covering all common SQL Server and Azure SQL transient error numbers |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollyAzureTableStorage](https://www.nuget.org/packages/PollyAzureTableStorage) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureTableStorage.svg)](https://www.nuget.org/packages/PollyAzureTableStorage) | Polly v8 resilience pipelines for Azure Table Storage — retry, timeout, and circuit-breaker for Azure.Data.Tables TableClient |
| [PollyChaos](https://www.nuget.org/packages/PollyChaos) | [![Downloads](https://img.shields.io/nuget/dt/PollyChaos.svg)](https://www.nuget.org/packages/PollyChaos) | Chaos engineering and fault-injection resilience strategies for Polly v8 pipelines |

## 💼 Need .NET consulting?

The author of this package is available for consulting on **Polly v8 resilience**, **Azure cloud architecture**, and **clean .NET design**.

**[→ solidqualitysolutions.com](https://www.solidqualitysolutions.com/)** · **[LinkedIn](https://www.linkedin.com/in/justbannister/)**
## License

MIT
