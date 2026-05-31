---
sidebar_position: 15
title: "PROP-015: Generated Discriminated Unions"
tags: [HelperUnions]
---

# PROP-015: Generated Discriminated Unions

**Status:** done  
**Size:** new-project  
**Created:** 2026-05-29  

## Problem / Motivation

DotnetHelpers provides rich functional programming abstractions:

- **Result** — success and failure
- **Option** — presence and absence
- **Async helpers** — safe composition over async boundaries
- **Source generators** — compile-time code generation
- **Roslyn analyzers** — compile-time validation

These solve operational outcomes but do not address domain modeling scenarios where a value represents one of several known variants:

```csharp
Customer | Supplier | Partner
Created | Updated | Deleted
CreditCard | BankTransfer | PayPal
```

Developers commonly model these using inheritance hierarchies, marker interfaces, or generic union types. These approaches lack:

- Exhaustive handling guarantees
- Strong discoverability
- Clear domain intent
- Compile-time safety

Named discriminated unions provide a more expressive and maintainable solution.

## Decisions

### Declaration Syntax

Unions are declared as `partial record` types with nested `sealed record` variants:

```csharp
[Union]
public partial record BusinessParty
{
    public sealed record Customer(CustomerInfo Info);

    public sealed record Supplier(SupplierInfo Info);

    public sealed record Partner(PartnerInfo Info, PartnerTier Tier);

    public sealed record Prospect();
}
```

Key rules:

- **`partial record` only** — `partial class` is not supported. Records provide value equality, `with` expressions, immutability signaling, and `ToString()` for free. These are all desirable properties for domain values.
- **No variant naming prefix** — Variants use plain names (`Customer`, not `AsCustomer`). The `As` prefix is already a .NET convention for type conversions and would conflict.
- **Variants support all arities** — zero fields, single field, multiple fields.

### Why Record

Records are the right choice because:

- **Value equality** — Two `BusinessParty.Customer(same)` are equal without manual `Equals`/`GetHashCode`.
- **Immutability** — DUs are values. Records communicate this intent.
- **Deconstruction** — Positional records deconstruct naturally in pattern matching.
- **ToString** — Free readable representation including variant data.
- **`with` expressions** — Enable transformations on variants.

### Internal Representation

The internal representation is an inheritance hierarchy. Each nested variant record inherits from the outer union type. The CLR type itself serves as the discriminator — no explicit tag field is needed.

This is dictated by the declaration syntax: nested `sealed record` types within a `partial record` base.

### Equality Semantics

Two union values are equal when they hold the same variant with the same payload. This follows directly from record equality semantics and requires no custom implementation.

### Generated API

#### Construction

```csharp
BusinessParty party = new BusinessParty.Customer(customerInfo);
```

#### Variant Inspection

```csharp
party.IsCustomer    // bool
party.IsSupplier    // bool
party.IsPartner     // bool
party.IsProspect    // bool
```

#### Variant Extraction

```csharp
party.TryGetCustomer(out var info)                  // bool
party.TryGetPartner(out var info, out var tier)      // bool
```

#### Match (Named Builder)

```csharp
var name = party.Match()
    .Customer(info => info.Name)
    .Supplier(info => info.CompanyName)
    .Partner((info, tier) => $"{info.Name} ({tier})")
    .Prospect(() => "Unknown")
    .Result();
```

Design properties:

- **Order-dependent** — variants must be handled in declaration order. Missing a variant is a compile error at `.Result()`.
- **Self-documenting** — variant names appear in the chain.
- **Unwrapped** — lambdas receive the payload fields directly, not the variant wrapper.
- **Type-safe exhaustiveness** — `.Result()` only exists on the builder type when all variants are covered.
- **No-payload shorthand** — variants with no fields accept a constant value:

```csharp
.Prospect("Unknown")
```

#### Match Async

```csharp
var name = await party.MatchAsync()
    .Customer(async info => await LoadDetails(info))
    .Supplier(async info => await LoadSupplier(info))
    .Partner(async (info, tier) => await LoadPartner(info, tier))
    .Prospect(() => Task.FromResult("Unknown"))
    .ResultAsync();
```

#### Switch

```csharp
party.Switch()
    .Customer(info => HandleCustomer(info))
    .Supplier(info => HandleSupplier(info))
    .Partner((info, tier) => HandlePartner(info, tier))
    .Prospect(() => LogProspect())
    .Execute();
```

Same builder pattern as Match, but returns `void` via `.Execute()`.

### Pattern Matching Support

Generated unions work with existing C# pattern matching:

```csharp
switch (party)
{
    case BusinessParty.Customer { Info: var info }:
        break;
    case BusinessParty.Supplier { Info: var info }:
        break;
    case BusinessParty.Partner { Info: var info, Tier: var tier }:
        break;
    case BusinessParty.Prospect:
        break;
}
```

### Adding a Variant — Loud Breaking

When a developer adds a new variant, the following breaks occur:

- **All `.Match().Result()` calls** — compile error.
- **All `.Switch().Execute()` calls** — compile error.
- **All C# `switch` statements/expressions** — analyzer warning (configurable to error via `.editorconfig`).

### Analyzer Support

#### DNHU0001: Non-Exhaustive Union Match

Fires when a `switch` statement or expression on a union type does not cover all variants and has no default/discard arm.

Default severity: **Warning**, configurable to Error via `.editorconfig`.

#### DNHU0003: Invalid Union Declaration

Fires when `[Union]` is applied to a type that is not a `partial record`.

Default severity: **Error**.

### Package Structure

```
dotnet add package HelperUnions
```

Single NuGet package bundles: attribute assembly, source generator, analyzer, and code fix provider.

## Sketch

See proposal body above for full design.

## Open Questions

- **Serialization strategy** — Deferred to V2; separate optional package preferred.
- **Generic variants** — Deferred to V2.
- **Code fix scope** — Resolved: fix targets both switch statements and switch expressions.

## Prior Art / References

- [F# Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions)
- [OneOf](https://github.com/mcintyre321/OneOf)
- [Thinktecture.Runtime.Extensions](https://github.com/PawelGerr/Thinktecture.Runtime.Extensions)
- [language-ext](https://github.com/louthy/language-ext)
- [Rust enums](https://doc.rust-lang.org/book/ch06-00-enums.html)
- [Swift enums](https://docs.swift.org)

## Outcome

Built HelperUnions V1 as a new NuGet package (`HelperUnions`) covering the full planned scope:

- **`[Union]` attribute** — marks `partial record` declarations as union types
- **Source generator** — emits abstract base, private constructor, `Is*` properties, `TryGet*` methods, and exhaustive `Match`/`Switch`/`MatchAsync`/`SwitchAsync` builder chains
- **DNHU0003 analyzer** — compile-time error when `[Union]` is applied to a non-partial or non-record type
- **DNHU0001 analyzer** — warning when a `switch` statement or expression on a union type does not cover all variants; handles `DeclarationPatternSyntax`, `TypePatternSyntax`, `RecursivePatternSyntax`, and `ConstantPatternSyntax` (Roslyn parses bare qualified nested type names as constant expressions)
- **Code fix provider** — "Add missing union variant arms" inserts `throw new System.NotImplementedException()` stubs for each missing variant in both switch expressions and statements
- **Single NuGet package** — bundles all four assemblies; consumers install with `dotnet add package HelperUnions`

Deferred to V2 (see [PROP-016](../active/016-helperunions-v2.md)):

- Implicit conversions (single-field variant → union type)
- System.Text.Json serialization support
- Generic union variants
- Order-independent Match builders
- DNHU0002 (discarded union value)

See: [ADR-011: Union Inheritance and Linear Match Builders](../../decisions/011-union-inheritance-and-linear-builders.md)
