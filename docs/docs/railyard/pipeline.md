---
sidebar_position: 2
title: Pipeline
description: Railyard's operation pipeline stages, error codes, validation, and serialization options
keywords:
  - railyard
  - pipeline
  - error codes
  - RY001
  - RY002
  - RY003
  - validation
  - serialization
---

# Pipeline

Every `DispatchAsync` call flows through a fixed, sequential pipeline. Each stage produces a `Result<T>`. A failure at any stage short-circuits the rest — no exceptions cross the boundary.

## Stages

```
name + JSON payload
       │
       ▼
  Resolve ──────► Result<IOperation>
       │             unknown name → RY002
       ▼
  Deserialize ──► Result<TInput>
       │             parse failure → RY001
       │             null result   → RY001
       ▼
  Validate ─────► Result<TInput>
       │             returned by your Validate() override
       ▼
  Execute ──────► Result<TOutput>
       │             returned by your ExecuteAsync() override
       ▼
  Serialize ────► Result<string>
                    serialization failure → RY003
```

## Error Codes

| Code | Stage | Meaning |
|------|-------|---------|
| `RY001` | Deserialize | Input JSON could not be parsed, or deserialized to `null` |
| `RY002` | Resolve | No operation is registered with the supplied name |
| `RY003` | Serialize | Output could not be serialized to JSON |

Error messages from `RY002` and `RY003` are contextualized with the operation name (e.g., `"Operation 'greet': Output serialization failed."`).

:::note

`RY001`–`RY003` are runtime error codes on the `Error` type. The compile-time diagnostic codes `RY1001`–`RY1003` are separate — see [Diagnostics](./diagnostics.md).

:::

## Validation

Override `Validate` to enforce business rules on the deserialized input before execution:

```csharp
protected override Result<GreetInput> Validate(GreetInput input)
{
    if (string.IsNullOrWhiteSpace(input.Name))
        return Result.Failure<GreetInput>(Error.Create("Name is required.", "GREET001"));

    return Result.Success(input);
}
```

The default `Validate` implementation passes the input through unchanged. Returning `Result.Failure` short-circuits execution — `ExecuteAsync` is never called.

## Cancellation

`CancellationToken` is threaded through `ExecuteAsync`. Throw `OperationCanceledException` from your execute method (e.g., via `ct.ThrowIfCancellationRequested()`) and it propagates out of `DispatchAsync` unchanged — it is never wrapped in a `Result`.

Deserialization and serialization also rethrow `OperationCanceledException` without wrapping.

## Serialization Options

By default the pipeline uses `System.Text.Json` with its default settings. To customize, register `JsonSerializerOptions` in the DI container:

```csharp
services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
});

services.AddRailyard();
```

The generated yard resolves `JsonSerializerOptions` from the **scoped** service provider created per dispatch. If no options are registered, JSON defaults apply.

:::tip

Input and output types are fully under your control. Records with primary constructors deserialize cleanly with `System.Text.Json` defaults and are recommended — but any `class` type that `System.Text.Json` can handle is valid.

:::

## Scoped Dispatch

Each call to `DispatchAsync` creates a dedicated DI scope. Operations are registered as **transient** and are resolved fresh every dispatch. This means:

- Scoped dependencies (e.g., `DbContext`, `HttpClient`) are isolated per call
- There is no ambient scope from the caller — the operation's scope is independent
- The scope is disposed when the dispatch completes

If you need to share state across multiple operations in a single logical request, do that at the caller level before dispatching — not via a shared DI scope.
