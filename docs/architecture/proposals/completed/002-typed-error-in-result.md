---
sidebar_position: 2
title: "PROP-002: Typed Error in Result<TData>"
tags: [Monads]
---

# PROP-002: Typed Error in Result&lt;TData&gt;

**Status:** done  
**Size:** large  
**Created:** 2026-05-25  

## Problem / Motivation

Currently `Result<TData>` hardcodes `Exception?` as the error type. This means:

- Consumers must catch/downcast to get specific error info
- You can't represent domain errors as simple value types (enums, records)
- Composition with business rule violations requires wrapping in exceptions
- Testing failure paths means constructing exception objects

A `Result<TData, TError>` would let consumers define their own error type.

## Sketch

Add a second generic: `Result<TData, TError>` alongside the existing `Result<TData>`.

```csharp
// Domain error as a simple discriminated union
public abstract record OrderError
{
    public record NotFound(Guid Id) : OrderError;
    public record InsufficientStock(string Sku, int Requested) : OrderError;
}

Result<Order, OrderError> PlaceOrder(OrderRequest req) { ... }
```

The existing `Result<TData>` becomes effectively `Result<TData, Exception>` (either as a type alias or thin wrapper) for backward compatibility.

Key additions to the API surface:

- `MapError<TNewError>(Func<TError, TNewError>)` for error transformation
- `Match<TOut>(Func<TData, TOut>, Func<TError, TOut>)` for exhaustive handling
- Implicit conversions or factory methods bridging `Result<TData>` ↔ `Result<TData, Exception>`

## Open Questions

- Keep both `Result<TData>` and `Result<TData, TError>`? Or migrate entirely?
- Should `TError` be constrained (e.g., `notnull`)?
- How does this affect the `Map`/`Bind` chain signatures?
- Impact on `BusinessRules.ResultExtensions` interop?
- Binary breaking change or new package?

## Prior Art / References

- Rust `Result<T, E>`
- F# `Result<'T, 'TError>`
- FluentResults, OneOf, ErrorOr (C# alternatives)
- Current implementation: `src/Monads/Result/Result.cs` (hardcoded `Exception?`)

## Outcome

Decided against adding a second generic parameter `Result<TData, TError>`. Instead, replace
the hardcoded `Exception?` with a lightweight `Error` record type:

```csharp
public record Error(string Message, string? Code = null, Exception? Exception = null);
```

Key decisions:
- **No generic error type** — avoids chain composition pain (`MapError` everywhere) and extension method explosion
- **Record** — value equality, immutability, `with` expressions
- **Minimal factories** — `Error.Create(...)` and `Error.Unexpected(Exception)` only; no opinionated common errors
- **`Code` is `string?`** — optional, not an enum; typed codes deferred to PROP-014
- **Single error per Result** — aggregation deferred to PROP-013 (OneOrMany)
- **Extensibility** — consumers add domain errors via their own static classes

See: [ADR-010: Error Record Over Exception](../../decisions/010-error-record-over-exception.md)  
Related: [PROP-013: OneOrMany Collection](../active/013-one-or-many-collection.md), [PROP-014: Source-Generated Typed Error Codes](../active/014-source-generated-error-codes.md)
