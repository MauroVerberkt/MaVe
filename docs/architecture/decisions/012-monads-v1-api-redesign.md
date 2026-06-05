---
sidebar_position: 12
title: "ADR-012: Monads V1 API Redesign"
tags: [Monads]
---

# ADR-012: Monads V1 API Redesign

**Status:** Accepted

## Context

The original Monads API had grown organically and accumulated several inconsistencies:

1. **Ambiguous naming** — `Bind` had two distinct meanings in the codebase: "chain an independent operation" (no data forwarded) and "chain with data" (monadic bind). In standard functional programming, `Bind` (also written `>>=`) always means the monadic bind — passing the wrapped value to the next function. Having `Bind` mean something else violated the principle of least surprise.

2. **Proliferation of near-identical methods** — `BindWithData`, `BindAndTransform`, and `Bind` all expressed chaining but differed only in whether data was forwarded and whether the type changed. Consumers had to remember three names for what is conceptually one or two operations.

3. **No exhaustive match on Result** — `Result<T>` provided `IsSuccess`/`IsFailure` checks and `Map`/`Bind`, but no `Match` method. Consumers who needed to derive a value from either branch had to use `if`/`else` or ternary expressions. `Option<T>` already had `Match`; the asymmetry was confusing.

4. **Option lacked functional combinators** — `Option<T>` only had `Match`/`MatchAsync`. It had no `Map`, `Bind`, `Select`, or `SelectMany`. Consumers wanting to transform an optional value had to unwrap and re-wrap manually, or use an awkward `Match(some: transform, none: () => Option<T>.None)` pattern.

5. **No LINQ support** — Neither type supported `Select`/`SelectMany`, so LINQ query syntax (`from x in ... select ...`) was unavailable. LINQ syntax is a readable alternative to deeply-nested `Bind` chains.

6. **`OptionNotPresentException` was redundant** — Two exception types existed for the same invalid-state scenario: `OptionIsNoneException` (correct) and `OptionNotPresentException` (legacy). Having two exceptions for the same condition was confusing and forced callers to catch both.

7. **Internal construction leaking** — `Some<TValue>` and `None<TValue>` had accessible constructors (primary constructor syntax). The intended construction path was always `Option<T>.Some(value)` / `Option<T>.None`, but the subtype constructors were technically callable. This was an implementation detail that should have been hidden.

## Decision

### 1. Rename `Bind` → `Then`, introduce `Bind<TNew>` as the true monadic bind

| Before | After | Semantics |
|--------|-------|-----------|
| `Bind(Func<Result<T>>)` | `Then(Func<Result<T>>)` | Chain a same-typed operation; does **not** pass data |
| `BindAsync(...)` | `ThenAsync(...)` | Async counterpart of `Then` |
| `BindAndTransform<TNew>(Func<T, Result<TNew>>)` | `Bind<TNew>(Func<T, Result<TNew>>)` | Standard monadic bind — passes data, may change type |
| `BindAndTransformAsync<TNew>(...)` | `BindAsync<TNew>(...)` | Async counterpart of `Bind<TNew>` |
| `BindWithData(Func<T, Result<T>>)` | *(removed)* | Subsumed by `Bind<TNew>` when `TNew = T` |
| `BindWithDataAsync(...)` | *(removed)* | Same |

`Then` is idiomatic for "do this next, regardless of what came before" (side-effectful sequencing). `Bind` is the standard Haskell/F# name for monadic bind, and now matches that contract precisely.

### 2. Add `Match<TResult>` to `Result<T>`

```csharp
TResult Match<TResult>(Func<TData, TResult> onSuccess, Func<Error, TResult> onFailure);
Task<TResult> MatchAsync<TResult>(Func<TData, Task<TResult>> onSuccess, Func<Error, Task<TResult>> onFailure);
Task<TResult> MatchAsync<TResult>(Func<TData, CancellationToken, Task<TResult>> onSuccess, Func<Error, CancellationToken, Task<TResult>> onFailure, CancellationToken ct);
```

`Match` mirrors `Option<T>.Match` semantics: exhaustively handle both branches and project to a single return value. Unlike `IsSuccess`/`IsFailure`, there is no way to forget a branch.

### 3. Add functional combinators to `Option<T>`

```csharp
Option<TNew> Map<TNew>(Func<TValue, TNew> transform);
Task<Option<TNew>> MapAsync<TNew>(Func<TValue, Task<TNew>> transform);
Task<Option<TNew>> MapAsync<TNew>(Func<TValue, CancellationToken, Task<TNew>> transform, CancellationToken ct);

Option<TNew> Bind<TNew>(Func<TValue, Option<TNew>> function);
Task<Option<TNew>> BindAsync<TNew>(Func<TValue, Task<Option<TNew>>> function);
Task<Option<TNew>> BindAsync<TNew>(Func<TValue, CancellationToken, Task<Option<TNew>>> function, CancellationToken ct);
```

`Map` transforms the wrapped value; `Bind` chains operations that themselves return `Option`. This matches the standard functional API and brings `Option<T>` to parity with `Result<T>`.

### 4. Add LINQ support to both types

```csharp
// Result<T>
Result<TNew> Select<TNew>(Func<TData, TNew> selector);
Result<TResult> SelectMany<TNew, TResult>(Func<TData, Result<TNew>> selector, Func<TData, TNew, TResult> resultSelector);

// Option<T>
Option<TNew> Select<TNew>(Func<TValue, TNew> selector);
Option<TResult> SelectMany<TNew, TResult>(Func<TValue, Option<TNew>> selector, Func<TValue, TNew, TResult> resultSelector);
```

`Select` and `SelectMany` are thin wrappers over `Map` and `Bind` respectively. Their only purpose is to satisfy the C# LINQ query expression pattern, enabling `from x in result select f(x)` syntax.

### 5. Remove `OptionNotPresentException`, keep only `OptionIsNoneException`

All invalid-state throws in `Option<T>` now use `OptionIsNoneException`. `OptionNotPresentException` is removed entirely from the public API.

### 6. Internalize `Some<TValue>` and `None<TValue>` constructors

Both subtype constructors are now `internal`. Consumers must use `Option<T>.Some(value)` and `Option<T>.None`. This enforces the intended construction contract and prevents accidental subtype instantiation.

### 7. Add `operator ==` / `operator !=` to both types

Both `Result<TData>` and `Option<TValue>` already implement `IEquatable<T>`. Operator overloads delegate to `Equals`, making `result1 == result2` and `option1 == option2` work without explicit `.Equals()` calls.

## Consequences

**Positive:**
- `Then`/`Bind` naming matches standard functional programming conventions — easier to reason about, consistent with prior art (F#, Haskell, Rust)
- `Match` on `Result<T>` closes a symmetry gap with `Option<T>` and encourages exhaustive handling
- `Option<T>` is now a first-class monad: consumers can build pipelines without unwrapping
- LINQ syntax is available for both types, offering a readable alternative to nested `Bind` chains
- One fewer exception type to learn, handle, and document
- Internal constructors prevent invalid `Some`/`None` construction outside the library

**Negative:**
- **Breaking change**: All usages of `Bind` (old no-data form) must be renamed to `Then`; `BindAndTransform` → `Bind<TNew>`; `BindWithData` → `Bind` (same type). The compiler will surface all affected call sites.
- The package is pre-1.0, so breaking changes are acceptable within the versioning policy.

## Alternatives Considered

- **Keep `Bind` for both meanings with overload resolution** — rejected because the two overloads differ only in whether the lambda receives a parameter, which is easy to misread and does not match the standard monad contract.
- **Add `flatMap` as an alias** — rejected; this project uses C# naming conventions, not Scala/Java. `Bind` is the .NET idiomatic name for monadic bind.
- **Keep `OptionNotPresentException` as an alias** — rejected; two exception types for the same condition adds cognitive overhead with no benefit. A single `OptionIsNoneException` is unambiguous.
