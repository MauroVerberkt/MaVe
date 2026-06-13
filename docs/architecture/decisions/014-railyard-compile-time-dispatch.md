---
sidebar_position: 14
title: "ADR-014: Railyard Compile-Time Operation Dispatch"
tags: [Railyard]
---

# ADR-014: Railyard Compile-Time Operation Dispatch

**Status:** Accepted

## Context

A recurring pattern at serialization boundaries: an external caller sends a string name
and a JSON payload and expects a JSON response. The dispatch host needs to route the call
to typed, validated code, handle deserialization and serialization, and propagate errors
without exceptions crossing the boundary.

Existing approaches have trade-offs:

- **Runtime reflection** — flexible but not trim/AOT-safe and provides no compile-time
  validation of operation names or signatures.
- **Manual registration** — explicit but adds ~50 lines of plumbing per operation for
  registration, deserialization boilerplate, and error translation.
- **Full messaging frameworks (MediatR, Wolverine)** — solve a different scope (in-process
  decoupling, messaging pipelines) and require significant ceremony for the targeted use case.

The requirement was a lightweight library where adding a new operation means writing exactly
one class — no separate registration step, no routing config, no serialization boilerplate.

## Decision

**Railyard uses a Roslyn incremental source generator** to produce all dispatch infrastructure
at compile time from classes annotated with `[Operation("name")]`.

### Fixed Pipeline

Every dispatch follows the same five stages:

```
Resolve → Deserialize → Validate → Execute → Serialize
```

Each stage returns `Result<T>`. A failure at any stage short-circuits the rest. No
exceptions cross the dispatch boundary (except `OperationCanceledException`, which is
intentionally rethrown).

### Generated Artifacts

For each assembly containing `[Operation]`-annotated classes, the generator emits:

1. `AddRailyard()` — a `IServiceCollection` extension method that registers all operations
   as transient and the `IYard` implementation as a singleton.
2. `GeneratedYard` — a sealed `IYard` implementation containing a compile-time dispatch
   table (`Dictionary<string, Func<IServiceProvider, IOperation>>`), a descriptor map,
   and the `Manifest` list.

No reflection is used at runtime. The routing table is a generated dictionary literal.

### Scoped Dispatch

Each `DispatchAsync` call creates a dedicated `IServiceScope`. Operations and their
dependencies are resolved from this scope and are independent of any ambient scope at
the call site. The scope is disposed when the dispatch completes.

### Type Constraints

Base classes constrain `where TInput : class` and `where TOutput : class`.

- `TInput` — any reference type that `System.Text.Json` can deserialize is valid.
  Records are recommended for immutability and clean constructor-based deserialization,
  but not required.
- `TOutput` — the class constraint avoids bare value types as JSON root values (`42`,
  `true`), which produce poor response shapes. Wrapping in a record is trivial and
  produces clean, extensible JSON.

### Synchronous Convenience

`SyncOperation<TInput, TOutput>` seals `ExecuteAsync` and delegates to an abstract
`Execute(TInput input)`. This avoids forcing `Task.FromResult` boilerplate on callers
with no I/O.

### Compile-Time Diagnostics

The generator reports three diagnostic errors:

| ID | Condition |
|----|-----------|
| RY1001 | Two or more operations share the same name — neither is generated |
| RY1002 | `[Operation]` applied to a class not inheriting `Operation<TInput, TOutput>` |
| RY1003 | Operation name does not match `^[A-Za-z][A-Za-z0-9_-]*$` |

### Serialization

`System.Text.Json` is used for both deserialization and serialization. Custom
`JsonSerializerOptions` can be supplied by registering them in the DI container; the
generated yard resolves them from the scoped provider per dispatch.

### Error Taxonomy

Three runtime error codes are defined on `RailyardErrors`:

| Code | Meaning |
|------|---------|
| RY001 | Input could not be deserialized, or deserialized to null |
| RY002 | No operation registered with the supplied name |
| RY003 | Output serialization failed |

Domain-level validation errors (e.g., required field missing) are returned by the
operation's `Validate` override and use caller-defined error codes. Railyard does not
prescribe a validation error taxonomy.

## Consequences

**Positive:**
- Adding a new operation requires exactly one class — no separate registration, routing,
  or wiring step.
- The dispatch table is plain generated code — fully inspectable, debuggable, and
  trim/AOT-compatible.
- Compile-time diagnostics catch name conflicts and misconfigured base classes before
  any code runs.
- The fixed pipeline is simple to reason about and test: each stage has a single
  responsibility and a `Result<T>` return type.
- Scoped dispatch ensures operation dependencies are isolated; no accidental shared state.

**Negative / Trade-offs:**
- The pipeline shape is fixed for v1. Cross-cutting behaviors (logging, timing,
  authorization) must be implemented by wrapping `IYard` or by manually composing in
  `ExecuteAsync`. A behavior pipeline is deferred to v2.
- Single-assembly discovery only. Multi-assembly scenarios (shared operation libraries)
  require multiple `AddRailyard()` calls or are deferred to a future version.
- `JsonSerializerOptions` is resolved from DI by type — only one instance can be
  registered. Per-operation serialization customization is not supported in v1.

## Alternatives Considered

- **Runtime reflection-based dispatch** — rejected. Not AOT-safe, no compile-time
  validation of operation names, no dispatch table visible in source.
- **Manual registration with a fluent builder** — rejected. Reduces ceremony only
  marginally; still requires a separate registration step per operation.
- **MediatR-style in-process dispatch** — rejected. Designed for in-process decoupling,
  not serialization boundaries. No JSON-boundary pipeline, no manifest, no compile-time
  name validation.
- **T4 / compile-time code generation outside Roslyn** — rejected. Roslyn incremental
  generators are the standard .NET mechanism; they integrate with the IDE and produce
  inspectable output.
