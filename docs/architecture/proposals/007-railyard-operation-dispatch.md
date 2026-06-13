---
sidebar_position: 7
title: "PROP-007: Railyard — Dynamic Operation Dispatch"
tags: [Railyard]
---

# PROP-007: Railyard — Dynamic Operation Dispatch

**Status:** building  
**Size:** new-project  
**Created:** 2026-05-25  

## Problem / Motivation

Several scenarios require dispatching to named operations at runtime — CLI tools, API gateways, plugin systems — where the set of operations isn't known at compile time by the host. The typical approaches are:

1. **Giant switch statement** — doesn't scale, violates OCP
2. **Manual registration** — tedious, easy to forget a handler
3. **MediatR-style** — powerful but heavy; requires marker interfaces per request/response pair

Railyard provides a lightweight middle ground: operations are self-describing, auto-discovered, and dispatched by name. The entire pipeline — parse → validate → execute — uses railway-oriented programming (`Result<T>`) so failures are explicit and composable.

## Sketch

### Core Concepts

| Metaphor | Type | Responsibility |
|----------|------|----------------|
| Yard | `Yard` / `IYard` | Registry. Discovers operations via reflection, registers them in DI, provides name-based lookup. |
| Operation | `Operation<TInput>` | A named unit of work. Accepts JSON string → deserializes to `TInput` record → validates → executes → returns `Result<string>`. |
| Info | `InfoDictionary` | Metadata catalogue. Maps operation names to human-readable descriptions. |

### Pipeline (per operation invocation)

```
JSON string
    │
    ▼
ParseInput ──► Result<TInput>
    │
    ▼
Validate ────► Result<TInput>
    │
    ▼
Execute ─────► Result<string>
```

Each step short-circuits on failure via `Result.Bind`.

### Defining an Operation

```csharp
internal class GreetOperation : Operation<GreetInput>
{
    private const string Name = "greet";
    private const string Info = "Greets the user by name.";

    protected override Result<GreetInput> Validate(GreetInput input)
        => string.IsNullOrWhiteSpace(input.Name)
            ? Result.Failure<GreetInput>(new Exception("Name is required."))
            : Result.Success(input);

    protected override Result<string> Execute(GreetInput input)
        => Result.Success($"Hello, {input.Name}!");
}

internal record GreetInput(string Name);
```

### Bootstrapping

```csharp
var services = new ServiceCollection();
// register your own services first...
var yard = new Yard(services); // discovers & registers all IOperation types

var result = yard.GetOperationByName("greet")
                 .Map(op => op.Perform("{\"name\":\"World\"}"));
```

### Key Design Decisions

- **String in, string out** — Operations sit at a serialization boundary. The caller doesn't need to know `TInput`; only the operation does.
- **Record constraint** — `TInput` must be a record (enforced at runtime). Records give free JSON deserialization semantics and immutability.
- **Reflection-based discovery** — Scans all loaded assemblies for `IOperation` implementations. Zero registration ceremony.
- **DI-integrated** — Operations are resolved from the container, so they can take constructor dependencies.
- **Option return for lookup** — `GetOperationByName` returns `Option<IOperation>`, not null or exception.

## Open Questions

- **Async support** — Current pipeline is synchronous. Should there be `IAsyncOperation` / `Operation<TInput>.ExecuteAsync`?
- **Output typing** — Everything returns `Result<string>`. Is there a use case for `Result<TOutput>` where the caller knows the output type?
- **Naming strategy** — Currently relies on a private `const string Name` field (reflection). Would an attribute (`[OperationName("greet")]`) be cleaner?
- **Assembly scanning scope** — `AppDomain.CurrentDomain.GetAssemblies()` may miss assemblies not yet loaded. Should callers pass explicit assemblies, or is lazy-loading acceptable?
- **Newtonsoft vs System.Text.Json** — Project currently uses Newtonsoft for deserialization. Align with System.Text.Json for consistency with the rest of the solution?
- **Trimming/AOT** — The `[RequiresUnreferencedCode]` annotation acknowledges this isn't trim-safe. Is that acceptable for intended use cases?

## Prior Art / References

- Railway-oriented programming (Scott Wlaschin) — the `Result` monad this builds on
- MediatR — similar dispatch-by-type pattern but request/response paired
- Wolverine — convention-based message handling with DI
- `HelperMonads.Result` / `HelperMonads.Option` — the underlying monads from this repo

## Outcome

_In progress on branch `railyard-introduction`._
