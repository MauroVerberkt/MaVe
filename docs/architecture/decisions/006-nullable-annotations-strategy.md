---
sidebar_position: 6
title: "ADR-006: Nullable Annotations Strategy"
tags: [Monads, BusinessRules]
---

# ADR-006: Nullable Annotations Strategy

**Status:** Accepted

## Context

C# nullable reference types (NRT) provide compile-time null safety, but require careful API design to be effective. The `Result<TData>` type has a fundamental challenge: `Data` is only valid when `IsSuccess` is true, and `Error` is only valid when `IsFailure` is true. Without proper annotations, consumers get null warnings regardless of how they check the result.

## Decision

Enable nullable reference types globally (`<Nullable>enable</Nullable>`) and use conditional null annotations:

```csharp
[MemberNotNullWhen(true, nameof(Data))]
[MemberNotNullWhen(false, nameof(Error))]
public bool IsSuccess { get; }
```

For `Option<T>`, use `[DoesNotReturn]` on the `None<T>.Value` getter to signal that accessing it always throws.

## Consequences

**Positive:**
- Zero null warnings when consumers branch on `IsSuccess`/`IsFailure` - the compiler narrows types automatically
- Self-documenting: the annotations express the invariant in code
- Catches incorrect usage at compile time (e.g., accessing `Data` without checking `IsSuccess`)
- `[DoesNotReturn]` on `None.Value` gives clear compiler feedback about unreachable code
- Enables flow analysis across method boundaries

**Negative:**
- Requires `<Nullable>enable</Nullable>` in consuming projects to get full benefit
- Annotations are invisible in IntelliSense (only visible in source)
- Older .NET versions (pre-6.0) may not fully support these attributes
- Slight learning curve for contributors unfamiliar with conditional null attributes
