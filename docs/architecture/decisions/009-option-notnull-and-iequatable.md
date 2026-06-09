---
sidebar_position: 9
title: "ADR-009: Add notnull Constraint and IEquatable to Option<TValue>"
tags: [Monads]
---

# ADR-009: Add notnull Constraint and IEquatable to Option&lt;TValue&gt;

**Status:** Accepted

## Context

`Option<TValue>` had two inconsistencies with the sibling `Result<TData>` type:

1. **No generic constraint** — consumers could write `Option<string?>` or `Option<int?>`, wrapping an already-nullable type in an Option. This defeats the purpose of Option (making nullability explicit via the type system rather than null values).

2. **No structural equality** — `Result<TData>` implements `IEquatable<Result<TData>>` with proper `Equals`/`GetHashCode`, but Option relied on default reference equality. This meant two `Some("hello")` instances were not equal, Options could not be reliably used as dictionary keys, and the behavior was inconsistent between the singleton `None` (reference equality happened to work) and freshly-allocated `Some` instances (it didn't).

## Decision

### Add `where TValue : notnull` to all Option types

```csharp
public abstract class Option<TValue> : IEquatable<Option<TValue>> where TValue : notnull { ... }
public sealed class Some<TValue>(...) : Option<TValue> where TValue : notnull { ... }
public sealed class None<TValue> : Option<TValue> where TValue : notnull { ... }
```

### Add `IEquatable<Option<TValue>>` to the base class

Equality semantics:
- Two `Some<TValue>` are equal if their values are equal (via `EqualityComparer<TValue>.Default`)
- Two `None<TValue>` are always equal
- `Some` never equals `None`
- `GetHashCode` is consistent with `Equals`

This mirrors exactly how `Result<TData>` handles equality.

## Consequences

**Positive:**
- Compile-time enforcement: `Option<string?>` is now a compiler error
- Consistency with `Result<TData>` — same constraint, same equality contract
- Options work correctly as dictionary keys and in collections
- Value types (`Option<int>`, `Option<Guid>`) and non-nullable reference types remain valid

**Negative:**
- Breaking change: consumers using nullable type arguments (e.g., `Option<string?>`) get a compile error. This is intentional — it was always misuse.
- Binary-breaking: adding an interface implementation changes the type's metadata. Package is pre-1.0, so this is acceptable.
