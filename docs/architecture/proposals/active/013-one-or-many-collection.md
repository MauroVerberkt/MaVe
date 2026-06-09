---
sidebar_position: 13
title: "PROP-013: OneOrMany Collection & Error Aggregation"
tags: [Monads]
---

# PROP-013: OneOrMany Collection & Error Aggregation

**Status:** idea  
**Size:** medium  
**Created:** 2026-05-29  

## Problem / Motivation

When `Result<TData>` needs to carry multiple errors (e.g., validation with several failures), the naive approach is `IReadOnlyList<Error>`. But this allocates an array for every failed result, even though the vast majority of failures have exactly one error.

More broadly, the "one item or many items" pattern appears throughout codebases (event handlers, middleware chains, validation results) and deserves a general-purpose solution.

## Sketch

A `OneOrMany<T>` struct that stores a single `T` inline, only allocating a backing collection when a second element is added:

```csharp
public readonly struct OneOrMany<T> : IReadOnlyList<T>
{
    // Single element stored inline (no allocation)
    // Array allocated only on Add when count > 1
}
```

Usage in Result (future):

```csharp
public OneOrMany<Error> Errors { get; }  // zero allocation for single-error case
public Error Error => Errors[0];          // convenience shorthand
```

### Design Questions

- Should it be a struct (zero-alloc for single) or a class (reference semantics)?
- Immutable or builder pattern for construction?
- Should it implement `IReadOnlyList<T>`, `IEnumerable<T>`, or a custom interface?
- How does it interact with LINQ?
- Should `Result<TData>` use it, or should Result stay single-error with a separate `Combine` method?

## Open Questions

- Is this a standalone type in Monads, or its own package?
- Performance benchmarks: what's the actual cost of the single-element array vs. the struct overhead?
- Does this overlap with `System.Collections.Immutable` patterns?

## Prior Art / References

- `ValueListBuilder<T>` in .NET runtime (internal, stack-allocated)
- `ImmutableArray<T>` (always allocates)
- `OneOf` library (different purpose but similar "avoid allocation" goal)
- Roslyn `SyntaxList<T>` (single-element optimization)

## Outcome

_Pending_
