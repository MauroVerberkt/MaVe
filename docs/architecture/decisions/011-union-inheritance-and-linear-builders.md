---
sidebar_position: 11
title: "ADR-011: Union Inheritance and Linear Match Builders"
tags: [HelperUnions]
---

# ADR-011: Union Inheritance and Linear Match Builders

**Status:** Accepted

## Context

PROP-015 established the goal of source-generated discriminated unions for C#. The implementation required several non-obvious design decisions spanning the internal representation, the exhaustive matching API, analyzer behavior, and packaging. This ADR records all key V1 decisions in one place.

## Decisions

### 1. CLR Inheritance as Discriminator

Union variants are nested `sealed record` types that extend the union's base record type. The generated base is an `abstract partial record` with a private constructor:

```csharp
[Union]
public partial record BusinessParty
{
    public sealed record Customer(CustomerInfo Info) : BusinessParty;
    public sealed record Supplier(SupplierInfo Info) : BusinessParty;
}
```

The CLR type itself serves as the discriminator — no explicit tag field is needed.

**Why:** CLR inheritance enables polymorphic assignment (`BusinessParty party = new BusinessParty.Customer(...)`), native C# pattern matching (`case BusinessParty.Customer:`) without custom language support, and zero-overhead type tests via the runtime's built-in type system.

**Why not a tag enum:** A tag enum requires synchronization between the enum and generated switch logic, and forfeits native pattern matching support. It also prevents using the union in `switch` expressions and statements naturally.

### 2. Linear Match Builder Chain

The exhaustive `Match`/`Switch` builders use a linear chain where each variant must be handled in declaration order:

```csharp
var name = party.Match()
    .Customer(info => info.Name)
    .Supplier(info => info.CompanyName)
    .Result();
```

Each `.Variant(...)` call advances to the next builder type. `.Result()` exists only on the fully-saturated final type — missing any variant is a compile error.

**Why linear over 2^N order-independent:** True order-independence requires one builder state per subset of covered variants. For N variants this is 2^N states. A 6-variant union produces 64 builder types. Linear scaling (N+1 types) is acceptable; exponential is not. IDE autocomplete guides the user through declaration order naturally.

**Why not runtime dictionary:** A compile-time API that makes incompleteness a type error is strictly safer than a runtime `InvalidOperationException` thrown when `.Execute()` or `.Result()` is called with missing handlers.

### 3. `readonly struct` Builders

All `__MatchBuilder_N`, `__SwitchBuilder_N`, `__MatchAsyncBuilder_N`, and `__SwitchAsyncBuilder_N` types are `readonly struct`.

**Why:** Builder instances exist only to guide the chain and carry accumulated handlers. Making them structs eliminates heap allocation per chain link. `readonly` prevents accidental mutation across copies.

### 4. Async Builders Avoid `async`/`await`

`MatchAsync` and `SwitchAsync` builders accept `Func<..., Task<TResult>>` or `Func<..., Task>` and forward the `Task` directly without wrapping in a state machine:

```csharp
// Generated — direct forwarding:
public Task<TResult> ResultAsync() => MatchCore();

// NOT generated — unnecessary state machine:
public async Task<TResult> ResultAsync() => await MatchCore();
```

**Why:** Wrapping `async`/`await` around an already-async lambda produces an unnecessary state machine allocation. Direct `Task` forwarding eliminates that overhead with identical observable behavior.

### 5. Zero-Payload Constant Shorthand Only at Position > 0

For zero-field variants, the builder exposes a constant overload (pass a value directly rather than a lambda). This overload is only generated for builder positions after the first variant:

```csharp
// Only generated at position > 0:
public __MatchBuilder_N<TResult> Prospect(TResult value)

// At position 0, only the lambda overload is generated:
public __MatchBuilder_1<TResult> Customer(Func<CustomerInfo, TResult> handler)
```

**Why:** At position 0 (the first variant), `TResult` has not yet been inferred by the compiler. Offering `TResult value` as the first overload creates generic inference ambiguity — the compiler cannot determine `TResult` from the argument alone without a preceding type-bearing call. At position > 0, `TResult` is already fixed by the preceding `.Variant<TResult>(...)` call.

### 6. `ConstantPatternSyntax` in the Exhaustiveness Analyzer

The DNHU0001 analyzer's `GetMatchedType` helper handles a third pattern case beyond `DeclarationPatternSyntax` and `TypePatternSyntax`:

```csharp
ConstantPatternSyntax constantPattern
    when semanticModel.GetSymbolInfo(constantPattern.Expression).Symbol is INamedTypeSymbol namedType
    => namedType,
```

**Why:** When a user writes a bare type pattern without a designator (`BusinessParty.Prospect => ...`) in a switch expression arm, Roslyn's parser classifies the qualified nested type name as a `ConstantPatternSyntax` (member access expression) rather than a `TypePatternSyntax`. This was confirmed empirically: removing the `TypePatternSyntax` branch alone did not cover bare type patterns, while the `ConstantPatternSyntax` branch did. The branch resolves the symbol from the expression and checks whether it is a named type symbol to avoid false positives for actual constant fields.

### 7. No Implicit Conversions in V1

Implicit conversions from variant payload types to the union type (`CustomerInfo → BusinessParty`) were explicitly excluded.

**Why:** Implicit conversions interact poorly with overload resolution. A method accepting `BusinessParty` would silently accept a `CustomerInfo` if that payload type is used exclusively inside a union variant, creating surprising behavior at call sites. The benefit — saving one `new BusinessParty.Customer(...)` — does not justify the risk. Deferred to V2 pending a design that mitigates the hazard (see PROP-016).

### 8. Single NuGet Package

All four source assemblies are bundled into one consumer-facing package:

```
lib/net8.0/HelperUnions.dll                         ← [Union] attribute
analyzers/dotnet/cs/HelperUnionsGenerator.dll        ← source generator
analyzers/dotnet/cs/HelperUnionsAnalyzer.dll         ← DNHU0001 + DNHU0003
analyzers/dotnet/cs/HelperUnionsFixProvider.dll      ← code fix for DNHU0001
```

**Why:** Consumers declare a union with `[Union]` and immediately get generation, exhaustiveness checking, and code fixes from one `dotnet add package HelperUnions` invocation. Separate packages would require manual coordination between the attribute and tooling assemblies while offering no additional flexibility to consumers.

## Consequences

**Positive:**

- Exhaustiveness is enforced at compile time for Match/Switch builders; warnings (configurable to errors) for native `switch` via DNHU0001
- Zero heap allocation on builder chains
- Single package installation with no extra setup
- Native C# `switch` expressions and statements work with generated union types without custom language support
- No private dependencies needed in the package (unlike BusinessRules which bundles `System.Text.Json`)

**Negative:**

- Variant declaration order matters for Match/Switch builder usage — adding a variant in the middle changes the call chain order for all existing usages
- Variants must explicitly write `: UnionType` in their declaration — the generator validates this but cannot enforce it syntactically before the build
- Implicit conversions are not available in V1; construction is always explicit
