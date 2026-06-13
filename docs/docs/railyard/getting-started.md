---
sidebar_position: 1
title: Getting Started with Railyard
description: Install Railyard, define your first operation, and dispatch a call end-to-end
keywords:
  - railyard
  - getting started
  - operation
  - dispatch
  - installation
---

# Getting Started with Railyard

## Installation

```bash
dotnet add package MaVe.Railyard
```

## Define an Operation

Create a class that inherits from `Operation<TInput, TOutput>` and mark it with `[Operation]`:

```csharp title="GreetOperation.cs"
using MaVe.Monads;
using MaVe.Railyard;

[Operation("greet", Description = "Greets a user by name.")]
public sealed class GreetOperation : Operation<GreetInput, GreetOutput>
{
    protected override Result<GreetInput> Validate(GreetInput input)
    {
        return string.IsNullOrWhiteSpace(input.Name)
            ? Result.Failure<GreetInput>(Error.Create("Name is required."))
            : Result.Success(input);
    }

    protected override async Task<Result<GreetOutput>> ExecuteAsync(GreetInput input, CancellationToken ct)
    {
        // Do work here — call services, hit I/O, etc.
        return Result.Success(new GreetOutput($"Hello, {input.Name}!"));
    }
}

public sealed record GreetInput(string Name);
public sealed record GreetOutput(string Message);
```

**Rules:**
- The class must inherit `Operation<TInput, TOutput>` or `SyncOperation<TInput, TOutput>`
- `TInput` and `TOutput` must be reference types (`class` constraint)
- The dispatch name must match `^[A-Za-z][A-Za-z0-9_-]*$`
- `Validate` is optional — the default passes input through unchanged
- `ExecuteAsync` is the only required override

:::tip

For operations with no I/O, inherit `SyncOperation<TInput, TOutput>` and override `Execute` instead:

```csharp
[Operation("ping")]
public sealed class PingOperation : SyncOperation<PingInput, PingOutput>
{
    protected override Result<PingOutput> Execute(PingInput input)
    {
        return Result.Success(new PingOutput("pong"));
    }
}
```

:::

## Register with DI

The source generator emits an `AddRailyard()` extension method that registers all discovered operations and the `IYard` implementation:

```csharp title="Program.cs"
var services = new ServiceCollection();
services.AddRailyard();
```

Operations can declare their own dependencies — inject them through the constructor as normal:

```csharp
[Operation("greet")]
public sealed class GreetOperation : Operation<GreetInput, GreetOutput>
{
    private readonly IGreetingService _greetingService;

    public GreetOperation(IGreetingService greetingService)
    {
        _greetingService = greetingService;
    }

    // ...
}
```

Each `DispatchAsync` call creates a dedicated DI scope. Operation dependencies are resolved fresh per dispatch and do not bleed across calls.

## Dispatch a Call

Resolve `IYard` and call `DispatchAsync`:

```csharp
var yard = serviceProvider.GetRequiredService<IYard>();

Result<string> result = await yard.DispatchAsync(
    operationName: "greet",
    jsonInput: """{"Name": "World"}""",
    ct: CancellationToken.None);

if (result.IsSuccess)
    Console.WriteLine(result.Data); // {"Message":"Hello, World!"}
```

Input and output are JSON strings. The pipeline handles deserialization and serialization — your operation code works with typed objects.

## Inspect the Manifest

`IYard.Manifest` exposes all registered operations. Use it to build tool definitions, help text, or validation before dispatch:

```csharp
IReadOnlyList<OperationDescriptor> manifest = yard.Manifest;

foreach (var descriptor in manifest)
    Console.WriteLine($"{descriptor.Name}: {descriptor.Description ?? "(no description)"}");

// Look up a specific operation before dispatching:
OperationDescriptor? descriptor = yard.TryGetDescriptor("greet");
if (descriptor is null)
    Console.WriteLine("Operation not found");
```

## Next Steps

- [Pipeline](./pipeline.md) — understand the pipeline stages, error codes, and serialization options
- [Diagnostics](./diagnostics.md) — compile-time diagnostics that catch misconfigured operations at build time
