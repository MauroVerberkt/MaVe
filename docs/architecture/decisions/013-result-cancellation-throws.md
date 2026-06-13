---
sidebar_position: 13
title: "ADR-013: Result and Option Cancellation Throws"
tags: [Monads]
---

# ADR-013: Result and Option Cancellation Throws

**Status:** Accepted

## Context

`Result<TData>` and `Option<TValue>` expose async combinators that accept a `CancellationToken`.

`Result<TData>` token-aware async combinators:

- `BindAsync<TNewData>(Func<TData, CancellationToken, Task<Result<TNewData>>>, CancellationToken)`
- `MapAsync<TNewData>(Func<TData, CancellationToken, Task<TNewData>>, CancellationToken)`
- `MatchAsync<TResult>(Func<TData, CancellationToken, Task<TResult>>, Func<Error, CancellationToken, Task<TResult>>, CancellationToken)`
- `ThenAsync(Func<CancellationToken, Task<Result<TData>>>, CancellationToken)`
- `ThenAsync<TNewData>(Func<CancellationToken, Task<Result<TNewData>>>, CancellationToken)`

`Option<TValue>` token-aware async combinators:

- `MatchAsync<TResult>(Func<TValue, CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>, CancellationToken)`
- `MapAsync<TNewValue>(Func<TValue, CancellationToken, Task<TNewValue>>, CancellationToken)`
- `BindAsync<TNewValue>(Func<TValue, CancellationToken, Task<Option<TNewValue>>>, CancellationToken)`

Before this decision, these methods passed the token through to delegates but did not perform an explicit early cancellation check. A cancelled token therefore relied on delegate behavior to observe cancellation.

## Decision

Cancellation remains an infrastructure control-flow signal, not a `Result` state or `Option` state.

All token-aware async combinators in `Result<TData>` and `Option<TValue>` now call:

```csharp
cancellationToken.ThrowIfCancellationRequested();
```

as the first statement in the method.

No cancellation-specific `Result` or `Option` state is added.

## Consequences

**Positive:**
- Aligns with standard .NET cancellation semantics (`OperationCanceledException` propagation).
- Provides immediate, consistent cancellation behavior without relying on delegate internals.
- Keeps `Result<TData>` binary (success/failure), avoiding API expansion to model cancellation.
- Keeps `Option<TValue>` binary (some/none), avoiding API expansion to model cancellation.

**Negative:**
- A pre-cancelled token now throws before existing short-circuiting. For example, a failed result with a cancelled token throws instead of returning the existing failure, and a `None` option with a cancelled token throws instead of returning `None`.
- Consumers that want cancellation represented as failure must opt in and map exceptions at their boundary.

## Alternatives Considered

- **Model cancellation as additional `Result`/`Option` states** — rejected due to API complexity and mismatch with .NET conventions.
- **No explicit early check, delegate-only observation** — rejected due to inconsistent behavior and delayed cancellation.
