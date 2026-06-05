---
sidebar_position: 14
title: "PROP-014: Source-Generated Typed Error Codes"
tags: [Monads]
---

# PROP-014: Source-Generated Typed Error Codes

**Status:** idea  
**Size:** large  
**Created:** 2026-05-29  

## Problem / Motivation

The `Error` record uses `string? Code` for machine-readable error classification. While flexible, this is stringly-typed — no compile-time safety, no IntelliSense, easy to typo.

Consumers who want type-safe error codes must currently rely on conventions (constants, static classes). A source generator could provide compile-time safe codes while keeping the ergonomic factory method pattern.

## Sketch

Consumer defines an enum in their project:

```csharp
[ErrorCodes] // Marker attribute triggers generator
public enum OrderErrorCode
{
    NotFound,
    InsufficientStock,
    PaymentDeclined,
    AlreadyShipped
}
```

Generator produces a typed error class:

```csharp
// Auto-generated
public static partial class OrderErrors
{
    public static Error NotFound(string message) => Error.Create(message, nameof(OrderErrorCode.NotFound));
    public static Error InsufficientStock(string message) => Error.Create(message, nameof(OrderErrorCode.InsufficientStock));
    // ...
}

// Or more ambitiously, a typed Error subtype:
public record OrderError : Error
{
    public OrderErrorCode TypedCode { get; }
    // ...
}
```

### Design Questions

- Generate factory methods (simple, `string` Code under the hood) or a typed `Error` subtype?
- How do consumers combine errors from different enum types in a single pipeline?
- Should the generator live in the Monads package or a separate analyzer package?
- What if the consumer doesn't use a generator — does `string? Code` still work seamlessly?

## Open Questions

- Is source generation overkill for this? Would a simple `Error.Create<TCode>(TCode code, string message) where TCode : Enum` suffice?
- How does this compose with `BusinessRules` (which already has its own code-gen)?
- What's the migration path from `string?` codes to generated codes?

## Prior Art / References

- `BusinessRulesGenerator` in this repo (existing source generator pattern)
- ErrorOr's `ErrorType` enum (baked into the library, not extensible)
- gRPC status codes (fixed enum, well-known set)
- Problem Details RFC 9457 (`type` URI for error classification)

## Outcome

_Pending_
