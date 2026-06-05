---
sidebar_position: 16
title: "PROP-016: Unions V2"
tags: [Unions]
---

# PROP-016: Unions V2

**Status:** idea  
**Size:** medium  
**Created:** 2026-05-31  

## Problem / Motivation

Unions V1 (PROP-015) shipped a complete, production-ready foundation for discriminated unions in C#. During implementation, several features were explicitly deferred as out of scope for V1 due to design complexity, potential hazards, or unclear demand. This proposal collects those deferred items for consideration as a cohesive V2 milestone.

## Sketch

### Implicit Conversions

Single-field variants would support implicit conversion from the payload type to the union:

```csharp
BusinessParty party = customerInfo;  // CustomerInfo → BusinessParty implicitly
```

**Deferred reason:** Implicit conversions interact poorly with overload resolution. A method accepting `BusinessParty` would silently accept a `CustomerInfo` if that type appears exclusively as a union payload. A safe design might require an explicit opt-in per union:

```csharp
[Union(AllowImplicit = true)]
public partial record BusinessParty { ... }
```

### Serialization Support

System.Text.Json round-trip support for union types:

```json
{ "$type": "Customer", "info": { "name": "Acme Corp" } }
```

Likely a separate `Unions.SystemTextJson` package to keep the core package free of private dependencies. The generator would emit `[JsonDerivedType]` attributes or a custom `JsonConverter<T>` for each union.

### Generic Union Variants

Allow variant types with generic type parameters:

```csharp
[Union]
public partial record OperationResult<T>
{
    public sealed record Success(T Value) : OperationResult<T>;
    public sealed record Failure(Error Reason) : OperationResult<T>;
}
```

**Deferred reason:** Requires the generator to propagate type parameters through all N+1 builder types and handle `where` constraints, significantly increasing generated code complexity.

### Order-Independent Match Builders

Allow variant handlers to be supplied in any order:

```csharp
var name = party.Match()
    .Supplier(info => info.CompanyName)   // any order
    .Customer(info => info.Name)
    .Result();
```

**Deferred reason:** True order-independence without exponential type explosion requires tracking covered variants in a type-level set. No clean, scalable approach was identified for V1. Potential directions: C# 13+ static abstract interfaces, or a source-generated phantom type set encoding.

### DNHU0002: Discarded Union Value

Opt-in diagnostic warning when a union-returning method's return value is ignored:

```csharp
GetBusinessParty();  // DNHU0002: Union value was never inspected
```

Should default to **hidden** or **suggestion** severity since fire-and-forget invocations are common and intentional in many patterns.

## Open Questions

- Should serialization be a separate package or opt-in via an attribute on the union declaration?
- For generic variants, should type constraints on variants be propagated into builder lambda signatures?
- Is there a viable compile-time approach to order-independent builders that avoids exponential type explosion?
- Should implicit conversions require an explicit opt-in attribute, or follow a convention (e.g., only for single-field variants)?

## Prior Art / References

- [PROP-015](../completed/015-generated-discriminated-unions.md) (completed) — V1 implementation and deferred items
- [ADR-011](../../decisions/011-union-inheritance-and-linear-builders.md) — Rationale for V1 decisions, including explicit deferral reasoning for each item
- [Thinktecture.Runtime.Extensions](https://github.com/PawelGerr/Thinktecture.Runtime.Extensions) — serialization approach reference
- [TypeShape](https://github.com/eiriktsarpalis/typeshape-csharp) — potential foundation for generic variant generation

## Outcome

_Filled when status changes to done/parked._
