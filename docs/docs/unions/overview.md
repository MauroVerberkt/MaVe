---
sidebar_position: 0
title: Unions Overview
description: Source-generated discriminated unions for C# with exhaustive matching and compile-time safety
keywords:
  - discriminated union
  - union type
  - pattern matching
  - source generator
  - exhaustive matching
---

# Unions Overview

**Unions** provides source-generated discriminated unions for C#. A union is a type that can be exactly one of several named variants — each variant carrying its own payload.

## What Is a Discriminated Union?

A discriminated union models "exactly one of N known alternatives". Unlike inheritance, the set of variants is closed: all possibilities are known at compile time, and every consumer must handle all of them.

```csharp
[Union]
public partial record BusinessParty
{
    public sealed record Customer(CustomerInfo Info) : BusinessParty;
    public sealed record Supplier(SupplierInfo Info) : BusinessParty;
    public sealed record Partner(PartnerInfo Info, PartnerTier Tier) : BusinessParty;
    public sealed record Prospect() : BusinessParty;
}
```

The generator produces: variant inspection properties, extraction methods, and exhaustive `Match`/`Switch` builder chains.

## When to Use a Union

| Scenario | Abstraction |
|----------|-------------|
| Operation may fail with an error | `Result<T>` |
| Value may not be present | `Option<T>` |
| Value is exactly one of N domain alternatives | Union |

Unions model **what something is**. Result and Option model **what happened while getting it**. These are orthogonal:

```csharp
Result<BusinessParty>          // operation may fail; success holds a domain variant
Option<BusinessParty>          // party may not exist; when present, it is a variant
```

## Key Properties

- **Exhaustive** — `Match` and `Switch` builders do not compile unless all variants are handled
- **Closed** — the variant set is fixed at declaration time
- **Zero overhead** — CLR type dispatch, no boxing, no reflection
- **Native pattern matching** — works with `switch` statements and expressions without custom language support
- **Single package** — one `dotnet add package MaVe.Unions` installs the attribute, generator, analyzer, and code fix provider

## See Also

- [Getting Started with Unions](./getting-started.md) — declare your first union
- [Match and Switch](./match-and-switch.md) — exhaustive handling patterns
- [Analyzers](./analyzers.md) — compile-time diagnostics (DNHU0001, DNHU0003)
