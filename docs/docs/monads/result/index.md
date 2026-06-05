---
sidebar_position: 1
title: Result Monad
description: Represent operation outcomes with explicit success/failure states using Result<T>
keywords:
  - result
  - monad
  - error handling
  - functional programming
  - railway oriented programming
  - map
  - bind
  - cancellation token
  - pattern matching
---

import ResultDemo from '@site/src/components/ResultDemo';

# Result&lt;T&gt; Monad

The `Result<TData>` class represents the outcome of an operation, encapsulating both success and failure states along with optional data or error information. This approach provides explicit handling of operation results with better control over error handling, transformation, and asynchronous workflows.

<ResultDemo title="Interactive Result Example" />

## Features

The `Result<TData>` class provides:

- **Success and Failure States**: Indicates if the operation was successful or failed
- **Data and Error Handling**: Holds data on success or error information on failure
- **Functional Operations**: Methods for transforming data (`Map`), chaining operations (`Then`, `Bind<TNew>`), exhaustive matching (`Match`), and performing side effects (`OnSuccess`, `OnFailure`)
- **Asynchronous Support**: Async versions of all transformation and chaining functions
- **LINQ Support**: `Select` and `SelectMany` enable LINQ query syntax over results

## Use Cases

- **Error Handling**: Cleanly represent and manage error states without throwing exceptions
- **Chaining Operations**: Chain multiple operations together with automatic failure propagation
- **Data Transformation**: Safely transform data on success, or propagate failures
- **Asynchronous Workflows**: Full support for async operations and I/O-bound tasks

## API Summary

| Member | Description |
|--------|-------------|
| `IsSuccess` | Returns `true` if the operation succeeded |
| `IsFailure` | Returns `true` if the operation failed |
| `Data` | The data from a successful operation |
| `Error` | The error from a failed operation |
| `Map` | Transforms data if successful |
| `MapAsync` | Async version of `Map` (with `CancellationToken` overload) |
| `Then` | Chains with another same-typed operation (does not pass data) |
| `ThenAsync` | Async version of `Then` (with `CancellationToken` overload) |
| `Bind<TNew>` | Chains with another operation, passing current data (may change type) |
| `BindAsync<TNew>` | Async version of `Bind<TNew>` (with `CancellationToken` overload) |
| `Match<TResult>` | Exhaustively handles both success and failure, returning a value |
| `MatchAsync<TResult>` | Async version of `Match` (with `CancellationToken` overload) |
| `Select<TNew>` | Transforms data (enables LINQ `select`) |
| `SelectMany<TNew, TResult>` | Chains results (enables LINQ `from … from`) |
| `operator ==` / `!=` | Value equality based on `Data` or `Error` |
| `OnSuccess` | Executes an action if successful |
| `OnFailure` | Executes an action if failed |
| `Tap` | Executes an action regardless of outcome (useful for logging) |
| `Deconstruct` | Deconstructs into `(IsSuccess, Data, Error)` for pattern matching |

## Basic Usage

```csharp title="BasicResult.cs"
public static Result<int> PerformOperation(bool isSuccess)
{
    if (isSuccess)
        return Result.Success(42);
    else
        return Result.Failure<int>(Error.Create("Something went wrong"));
}

// Check the result
var result = PerformOperation(true);
if (result.IsSuccess)
{
    Console.WriteLine($"Operation succeeded with data: {result.Data}");
}
else
{
    Console.WriteLine($"Operation failed with error: {result.Error.Message}");
}
```

:::tip[Null Safety]

The `Result<T>` type uses `[MemberNotNullWhen]` attributes, so the compiler knows:
- When `IsSuccess` is `true`, `Data` is guaranteed non-null
- When `IsFailure` is `true`, `Error` is guaranteed non-null

:::

## Transformation and Chaining

Chain operations together to build pipelines that short-circuit on failure:

```csharp title="AsyncChaining.cs"
public static async Task ExampleAsync()
{
    var result = await PerformOperationAsync(true);

    var transformedResult = await result
        .MapAsync(async data => await Task.FromResult(data * 2))
        .ThenAsync(async () => await AnotherAsyncOperation());

    if (transformedResult.IsSuccess)
    {
        Console.WriteLine($"Transformed result: {transformedResult.Data}");
    }
    else
    {
        Console.WriteLine(transformedResult.ToString());
    }
}

public static Task<Result<int>> PerformOperationAsync(bool isSuccess)
{
    if (isSuccess)
        return Task.FromResult(Result.Success(42));
    else
        return Task.FromResult(Result.Failure<int>(Error.Create("Async operation failed")));
}

public static async Task<Result<int>> AnotherAsyncOperation()
{
    return await Task.FromResult(Result.Success(100));
}
```

## Real-World Pattern

```csharp title="UserService.cs"
public Result<UserDto> GetUserProfile(int userId)
{
    return GetUser(userId)
        .Map(user => new UserDto
        {
            Name = user.Name,
            Email = user.Email
        })
        .OnSuccess(dto => _cache.Set($"user:{userId}", dto))
        .OnFailure(error => _logger.LogWarning("User {Id} not found: {Error}", userId, error.Message));
}
```

## Tap — Side Effects Regardless of Outcome

`Tap` executes an action on the result regardless of success or failure, then returns the result unchanged. Useful for logging, metrics, or tracing in the middle of a pipeline:

```csharp title="TapExample.cs"
public Result<Order> ProcessOrder(OrderRequest request)
{
    return ValidateOrder(request)
        .Tap(r => _logger.LogInformation("Validation result: {Success}", r.IsSuccess))
        .Bind(order => SaveOrder(order))
        .Tap(r => _metrics.RecordOrderAttempt(r.IsSuccess));
}
```

## Deconstruct — Pattern Matching

`Deconstruct` enables C# deconstruction syntax, letting you extract all components in a single assignment:

```csharp title="DeconstructExample.cs"
var (success, data, error) = GetUser(userId);

if (success)
{
    Console.WriteLine($"Found user: {data!.Name}");
}
else
{
    Console.WriteLine($"Failed: {error!.Message}");
}
```

## Match — Exhaustive Handling

`Match` forces you to handle both success and failure, returning a value from each branch. Unlike `IsSuccess`/`IsFailure` checks, `Match` is exhaustive — the compiler ensures both cases are covered:

```csharp title="MatchExample.cs"
Result<User> result = GetUser(userId);

string message = result.Match(
    onSuccess: user => $"Welcome, {user.Name}!",
    onFailure: error => $"Error: {error.Message}"
);
```

Use `MatchAsync` when the handlers need to perform async work:

```csharp title="MatchAsyncExample.cs"
string response = await GetUser(userId).MatchAsync(
    onSuccess: async user => await RenderProfileAsync(user),
    onFailure: async error => await RenderErrorPageAsync(error)
);
```

## Then vs Bind — Chaining Patterns

Two chaining methods cover different composition scenarios:

**`Then` — chain an independent operation (no data forwarded)**

Use `Then` when the next step does not need the current result's data:

```csharp title="ThenExample.cs"
public Result<Order> FulfillOrder(int orderId)
{
    return GetOrder(orderId)
        .Then(() => CheckSystemAvailability())    // doesn't need the order
        .Bind(order => ValidateInventory(order))  // does need the order
        .Bind(order => ChargePayment(order));
}
```

**`Bind<TNew>` — chain and pass data (may change type)**

Use `Bind<TNew>` when the next step receives the current data. The type parameter is inferred from the return type:

```csharp title="BindExample.cs"
public Result<Shipment> CreateShipment(int orderId)
{
    return GetOrder(orderId)                          // Result<Order>
        .Bind(order => ValidateInventory(order))      // Result<Order>  (same type)
        .Bind(order => BuildShipment(order));         // Result<Shipment> (type changes)
}
```

## LINQ Query Syntax

`Select` and `SelectMany` enable LINQ query syntax, which can read more naturally for multi-step pipelines:

```csharp title="LinqExample.cs"
var result =
    from user in GetUser(userId)
    from profile in GetProfile(user.ProfileId)
    select $"{user.Name}: {profile.Bio}";
```

This is equivalent to:

```csharp
var result = GetUser(userId)
    .Bind(user => GetProfile(user.ProfileId)
        .Map(profile => $"{user.Name}: {profile.Bio}"));
```

## CancellationToken Support

All async operations have overloads that accept a `CancellationToken`, enabling proper cancellation propagation through result chains:

```csharp title="CancellationTokenExample.cs"
public async Task<Result<OrderConfirmation>> ProcessOrderAsync(
    OrderRequest request, CancellationToken ct)
{
    return await ValidateOrderAsync(request, ct)
        .BindAsync(
            async (order, token) => await SaveOrderAsync(order, token), ct)
        .BindAsync(
            async (order, token) => await ConfirmOrderAsync(order, token), ct);
}
```

Available `CancellationToken` overloads:
- `MapAsync(Func<TData, CancellationToken, Task<TNew>>, CancellationToken)`
- `ThenAsync(Func<CancellationToken, Task<Result<TData>>>, CancellationToken)`
- `BindAsync<TNew>(Func<TData, CancellationToken, Task<Result<TNew>>>, CancellationToken)`
- `MatchAsync<TResult>(Func<TData, CancellationToken, Task<TResult>>, Func<Error, CancellationToken, Task<TResult>>, CancellationToken)`

:::info

The Result pattern is a powerful tool for managing operation results, improving error handling, and enabling functional approaches to handling success/failure states in C#. It works seamlessly with both synchronous and asynchronous operations.

:::

## See Also

- [Option Monad](../option/index.md) - For values that may or may not exist
- [Business Rules + Result](../../business-rules/result-extensions.md) - Combining with validation
