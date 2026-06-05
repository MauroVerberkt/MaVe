---
sidebar_position: 7
title: "PROP-007: Railyard — Compile-Time Operation Dispatch"
tags: [Railyard]
---

# PROP-007: Railyard — Compile-Time Operation Dispatch

**Status:** exploring  
**Size:** new-project  
**Created:** 2026-05-25  

## Problem / Motivation

A recurring pattern exists at serialization boundaries: something external sends a
name and a payload, and you need to route it to typed, validated code. Examples:

- **Hardware gateways** — TCP messages dispatched to device actions
- **Tool orchestrators** — MCP tool calls routed to implementations (code, CLI tools, APIs)
- **CLI applications** — subcommands dispatched to handlers
- **Message processors** — queue messages routed by type

The typical experience: the *interesting* code for a new operation is 10 lines, but
the *plumbing* — registration, deserialization, routing, error translation — is 50.
Every framework that solves this either relies on runtime reflection (not trim/AOT-safe,
no compile-time validation) or requires heavy ceremony per operation (marker interfaces,
manual registration, request/response type pairs).

**Railyard's thesis:** if you write one class with a declared name, input shape, and
execute method, everything else should be generated at compile time — the DI registration,
the dispatch routing, the input schema, and the tool manifest.

## Core Concepts

| Metaphor | Role |
|----------|------|
| **Yard** | Generated dispatch registry. Maps operation names to handlers, provides manifest of available operations. |
| **Operation** | A named unit of work. Declares its name, input shape, and pipeline (validate → execute). |
| **Manifest** | Auto-generated metadata: operation name and description (v1), with JSON schema for input planned for v2. Consumable as MCP tool definitions, CLI help text, or API docs. |

## Pipeline

Each operation invocation flows through a railway-oriented pipeline:

```
name + JSON payload
       │
       ▼
  Resolve ──────► Result<Operation>      (name lookup — compile-time dispatch table;
       │                                   unknown name → Result.Failure with specific error)
       ▼
  Deserialize ──► Result<TInput>        (JSON → typed record)
       │
       ▼
  Validate ─────► Result<TInput>        (business rules)
       │
       ▼
  Execute ──────► Result<TOutput>       (the actual work)
       │
       ▼
  Serialize ────► Result<string>        (typed output → JSON response)
```

Every step short-circuits on failure via `Result.Bind`. No exceptions cross the boundary.

## Developer Experience

### Defining an Operation

```csharp
[Operation("greet", Description = "Greets the user by name.")]
public class GreetOperation : Operation<GreetInput, GreetOutput>
{
    protected override Result<GreetInput> Validate(GreetInput input)
        => string.IsNullOrWhiteSpace(input.Name)
            ? Result.Failure<GreetInput>(Errors.Required(nameof(input.Name)))
            : Result.Success(input);

    protected override async Task<Result<GreetOutput>> Execute(GreetInput input, CancellationToken ct)
        => Result.Success(new GreetOutput($"Hello, {input.Name}!"));
}

public record GreetInput(string Name);
public record GreetOutput(string Message);
```

That's it. No registration, no routing config, no serialization boilerplate.

### Bootstrapping

```csharp
services.AddRailyard(); // Generated extension method — registers all discovered operations
```

### Dispatching

```csharp
var yard = serviceProvider.GetRequiredService<IYard>();

Result<string> result = await yard.DispatchAsync("greet", """{"name": "World"}""", ct);
```

### Getting the Manifest

```csharp
IReadOnlyList<OperationDescriptor> manifest = yard.Manifest;
// Each descriptor: Name, Description (v1); InputSchema added in v2

// Lookup by name for pre-dispatch validation
OperationDescriptor? descriptor = yard.TryGetDescriptor("greet");
```

## Source Generation Strategy

The Roslyn source generator:

1. **Discovers** all classes with `[Operation]` attribute
2. **Validates** at compile time:
   - Name is unique across the assembly
   - Required overrides are present
3. **Emits:**
   - `AddRailyard()` extension method with all DI registrations (operations registered as **transient**)
   - Dispatch table (name → type mapping, no reflection at runtime)
   - `OperationDescriptor` instances (name + optional description in v1; JSON schemas in v2)
   - Diagnostic errors for violations (missing attribute, duplicate names)

**DI lifetime:** V1 registers all operations as transient. Per-operation lifetime
override via attribute (e.g., `[Operation("x", Lifetime = ServiceLifetime.Scoped)]`)
is deferred to a future version.

**`Description` property:** Optional on the `[Operation]` attribute. When omitted,
the manifest entry has a null/empty description.

**Note on type constraints:** The base class constrains `where TInput : class` and
`where TOutput : class`. Records are recommended (immutability, clean deserialization)
but not enforced — any class that `System.Text.Json` can deserialize is valid. This
avoids friction when consumers have existing domain types that don't fit the record shape.

The `TOutput` constraint exists because operations sit at a serialization boundary.
Bare value types as JSON root values (`42`, `true`) are technically valid but produce
poor response shapes — wrapping in a record (`record CountOutput(int Count)`) costs
nothing, produces clean JSON, and allows adding fields later without breaking consumers.

## Async-First Design

The primary pipeline is async (`Task<Result<TOutput>>`). A synchronous convenience
exists but the expectation is most operations hit I/O (hardware, external tools, APIs).

```csharp
// Async (primary)
public abstract class Operation<TInput, TOutput> : IOperation
    where TInput : class
    where TOutput : class
{
    protected virtual Result<TInput> Validate(TInput input) => Result.Success(input);
    protected abstract Task<Result<TOutput>> Execute(TInput input, CancellationToken ct);
}

// Sync convenience
public abstract class SyncOperation<TInput, TOutput> : Operation<TInput, TOutput>
{
    protected sealed override Task<Result<TOutput>> Execute(TInput input, CancellationToken ct)
        => Task.FromResult(ExecuteSync(input));

    protected abstract Result<TOutput> ExecuteSync(TInput input);
}
```

## Typed Output

Operations declare both `TInput` and `TOutput`. The framework serializes `TOutput` to
the JSON response string at the boundary. This gives:

- Type safety within the operation
- Typed composition when operations call each other in-process
- String serialization only at the external boundary

**In-process composition:** For operations that need to invoke other operations directly,
inject the operation via DI and call it with typed inputs — no need for a special
"typed dispatch" API. The `IYard.DispatchAsync` boundary is for external callers
(string in, string out). Internal callers bypass serialization entirely.

## Middleware / Behaviors (Future)

Pipeline behaviors wrap operations for cross-cutting concerns:

```csharp
public class TimingBehavior<TInput, TOutput> : IOperationBehavior<TInput, TOutput>
{
    public async Task<Result<TOutput>> Handle(
        TInput input,
        Func<Task<Result<TOutput>>> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = await next();
        Log.Information("Operation took {Elapsed}ms", sw.ElapsedMilliseconds);
        return result;
    }
}
```

Registered globally or per-operation. Generated into the pipeline at compile time or
resolved from DI at runtime (TBD).

**Stretch goal — custom pipeline shapes:** Allow consumers to define their own base
classes with additional pipeline steps (e.g., Deserialize → Normalize → Validate →
Execute). The generator would introspect the base class to wire up the pipeline. This
is powerful but significantly increases generator complexity — deferred until the fixed
pipeline proves too limiting in practice.

## Target Use Cases

### 1. Tool Orchestrator (MCP)

Railyard sits between MCP skill declarations and actual tool implementations. The
manifest generates MCP-compatible tool definitions. Incoming tool calls dispatch
through the pipeline. Implementations can be C# code, external CLI invocations, or
API calls.

### 2. Hardware Gateway

TCP/serial messages arrive as `name + payload`. Railyard dispatches to hardware
operation handlers. Adding a new hardware action = one class.

### 3. Background Job Processor

Message queue delivers typed jobs. Railyard provides the dispatch, validation, and
error-handling pipeline without requiring a full framework like Hangfire or Wolverine.

## Non-Goals / Out of Scope

- **Not a messaging framework** — no retries, dead-letter queues, sagas, or delivery
  guarantees. Railyard dispatches; transport is someone else's problem.
- **Not an HTTP framework** — no routing, no ASP.NET middleware pipeline. It sits
  *behind* whatever transport layer you use.
- **Not a CQRS framework** — no read/write separation concepts. Operations are
  operations; consumers can layer their own semantics on top.
- **Not a replacement for MediatR in existing architectures** — different design goals.
  Railyard targets serialization boundaries with compile-time generation; MediatR
  targets in-process decoupling with runtime resolution.

## Versioning Roadmap

### V1 — Core Dispatch

- Fixed pipeline: Deserialize → Validate → Execute → Serialize
- Source-generated dispatch table and DI registration
- Manifest with name + description per operation
- Async-first with CancellationToken support
- Single-assembly discovery
- Serialization via System.Text.Json (no schema generation)
- Target: .NET 8+

### V2 — Schema & Behaviors

- Compile-time JSON schema generation from TInput properties
- Pipeline behaviors (cross-cutting concerns)
- Schema available in manifest for MCP tool definitions, API docs

### Future

- Multi-assembly operation discovery
- Custom pipeline shapes (custom base classes)
- Streaming output support

## Open Questions

- **Output serialization** — Always JSON via System.Text.Json? Or pluggable serializers?
- **Behaviors registration** — Compile-time woven vs. runtime resolved? Compile-time
  is faster but less flexible.
- **Error taxonomy** — Should Railyard define standard error codes/categories
  (validation, not-found, unauthorized, infrastructure) or leave that to consumers?
- **Cancellation threading** — CancellationToken flows through Execute. Should it also
  flow through behaviors? Through Validate? (Validate should be fast/synchronous, so
  probably not.)
- **Streaming** — Some operations (especially tool orchestration) may want to stream
  output. Does that fit the Result model or need a separate path?

## Prior Art / References

- **Railway-oriented programming** (Scott Wlaschin) — the Result monad composition model
- **MediatR** — dispatch-by-type, behaviors pipeline. Heavy on ceremony, no schema generation.
- **Wolverine** — convention-based message handling. Heavier, targets full messaging scenarios.
- **ASP.NET Minimal APIs** — source-generated request binding (similar compile-time philosophy)
- **MCP Tool Protocol** — name + JSON schema + execute. Railyard's manifest aligns naturally.
- **Monads** — `Result<T>` and `Option<T>` from this repo, the foundation Railyard builds on.

## Outcome

_Exploring. Capturing the full vision before implementation begins._
