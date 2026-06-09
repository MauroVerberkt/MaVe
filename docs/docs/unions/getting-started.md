---
sidebar_position: 1
title: Getting Started with Unions
description: Declare your first discriminated union and use its generated API
keywords:
  - union
  - getting started
  - declaration
  - source generator
---

# Getting Started with Unions

## Installation

```bash
dotnet add package MaVe.Unions
```

## Declare a Union

Mark a `partial record` with `[Union]` and add `sealed record` variants as nested types:

```csharp title="BusinessParty.cs"
using MaVe.Unions;

[Union]
public partial record BusinessParty
{
    public sealed record Customer(CustomerInfo Info) : BusinessParty;
    public sealed record Supplier(SupplierInfo Info) : BusinessParty;
    public sealed record Partner(PartnerInfo Info, PartnerTier Tier) : BusinessParty;
    public sealed record Prospect() : BusinessParty;
}
```

**Rules:**
- Must be `partial record` — not `partial class` or `partial struct`
- Each variant must explicitly inherit from the union type (`: BusinessParty`)
- Variants can have zero, one, or multiple fields

The generator runs at compile time and emits the full API alongside your declaration.

## Construct a Value

```csharp
BusinessParty party = new BusinessParty.Customer(customerInfo);
BusinessParty prospect = new BusinessParty.Prospect();
```

## Inspect the Variant

```csharp
if (party.IsCustomer)
    Console.WriteLine("It's a customer");

if (party.IsPartner)
    Console.WriteLine("It's a partner");
```

## Extract the Payload

```csharp
if (party.TryGetCustomer(out var info))
    Console.WriteLine(info.Name);

// Multi-field variants — each field is a separate out parameter:
if (party.TryGetPartner(out var info, out var tier))
    Console.WriteLine($"{info.Name} ({tier})");
```

## Match Exhaustively

Use the `Match` builder to transform a union value. The chain will not compile unless all variants are covered:

```csharp
var displayName = party
    .Match()
    .Customer(info => info.Name)
    .Supplier(info => info.CompanyName)
    .Partner((info, tier) => $"{info.Name} ({tier})")
    .Prospect("Unknown")
    .Result();
```

See [Match and Switch](./match-and-switch.md) for the full API including async support.

## Use Native Pattern Matching

Generated unions work with `switch` statements and expressions natively:

```csharp
var displayName = party switch
{
    BusinessParty.Customer { Info: var info } => info.Name,
    BusinessParty.Supplier { Info: var info } => info.CompanyName,
    BusinessParty.Partner { Info: var info, Tier: var tier } => $"{info.Name} ({tier})",
    BusinessParty.Prospect => "Unknown",
};
```

The DNHU0001 analyzer warns when a `switch` over a union type is missing variants. See [Analyzers](./analyzers.md).

## Value Equality

Union values are records, so equality is structural:

```csharp
var a = new BusinessParty.Customer(new CustomerInfo("Acme"));
var b = new BusinessParty.Customer(new CustomerInfo("Acme"));

Console.WriteLine(a == b); // true
```

## Adding a Variant

When you add a new variant, all existing `Match().Result()` and `Switch().Execute()` chains stop compiling until the new variant is handled. This is intentional — adding a variant should be loud.

:::tip

Install the `Unions` package once and you immediately get: the `[Union]` attribute, the source generator, the DNHU0001/DNHU0003 analyzers, and the "Add missing variants" code fix — all from a single package reference.

:::
