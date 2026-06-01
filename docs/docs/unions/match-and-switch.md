---
sidebar_position: 2
title: Match and Switch
description: Exhaustive handling of union variants using Match and Switch builder chains
keywords:
  - match
  - switch
  - exhaustive
  - builder
  - async
---

# Match and Switch

HelperUnions generates four exhaustive builder APIs for every union type: `Match`, `Switch`, `MatchAsync`, and `SwitchAsync`.

## Match — Transform to a Value

`Match` transforms a union into a result value. The chain does not compile unless all variants are handled.

```csharp
var displayName = party
    .Match()
    .Customer(info => info.Name)
    .Supplier(info => info.CompanyName)
    .Partner((info, tier) => $"{info.Name} ({tier})")
    .Prospect(() => "Unknown")
    .Result();
```

**Properties:**
- Variants must be handled in **declaration order**
- Lambdas receive the payload fields directly (not the variant wrapper)
- `.Result()` is only available on the fully-saturated builder type — a missing variant is a compile error

### Zero-Payload Shorthand

Variants with no fields accept a constant value directly:

```csharp
.Prospect("Unknown")   // equivalent to .Prospect(() => "Unknown")
```

This shorthand is available for all zero-payload variants except the first one (to avoid type inference ambiguity).

## Switch — Execute Side Effects

`Switch` runs an action for each variant. Returns `void` via `.Execute()`.

```csharp
party
    .Switch()
    .Customer(info => SendWelcomeEmail(info))
    .Supplier(info => NotifyProcurement(info))
    .Partner((info, tier) => AssignAccountManager(info, tier))
    .Prospect(() => AddToLeadQueue())
    .Execute();
```

Same exhaustiveness guarantees as `Match`.

## MatchAsync — Async Transform

All handlers return `Task<TResult>`. Chain is awaited at `.ResultAsync()`.

```csharp
var summary = await party
    .MatchAsync()
    .Customer(async info => await LoadCustomerDetails(info))
    .Supplier(async info => await LoadSupplierDetails(info))
    .Partner(async (info, tier) => await LoadPartnerSummary(info, tier))
    .Prospect(() => Task.FromResult("No details"))
    .ResultAsync();
```

Handlers that don't need async can return `Task.FromResult(...)` directly. No unnecessary `async`/`await` state machines are generated — `Task` values are forwarded directly.

## SwitchAsync — Async Side Effects

All handlers return `Task`. Awaited at `.ExecuteAsync()`.

```csharp
await party
    .SwitchAsync()
    .Customer(async info => await SendWelcomeEmailAsync(info))
    .Supplier(async info => await NotifyProcurementAsync(info))
    .Partner(async (info, tier) => await AssignAccountManagerAsync(info, tier))
    .Prospect(() => Task.CompletedTask)
    .ExecuteAsync();
```

## Native Pattern Matching

Generated unions are plain C# record types, so native `switch` expressions and statements work out of the box:

```csharp
// Switch expression
var name = party switch
{
    BusinessParty.Customer { Info: var info } => info.Name,
    BusinessParty.Supplier { Info: var info } => info.CompanyName,
    BusinessParty.Partner { Info: var info, Tier: var tier } => $"{info.Name} ({tier})",
    BusinessParty.Prospect => "Unknown",
};

// Switch statement
switch (party)
{
    case BusinessParty.Customer { Info: var info }:
        HandleCustomer(info);
        break;
    case BusinessParty.Supplier { Info: var info }:
        HandleSupplier(info);
        break;
    case BusinessParty.Partner { Info: var info, Tier: var tier }:
        HandlePartner(info, tier);
        break;
    case BusinessParty.Prospect:
        HandleProspect();
        break;
}
```

The [DNHU0001 analyzer](./analyzers.md) warns when a `switch` over a union type does not cover all variants.

## Comparison

| API | Return type | Use when |
|-----|-------------|----------|
| `Match().Result()` | `TResult` | Synchronous transform to a value |
| `Switch().Execute()` | `void` | Synchronous side effects |
| `MatchAsync().ResultAsync()` | `Task<TResult>` | Async transform to a value |
| `SwitchAsync().ExecuteAsync()` | `Task` | Async side effects |
| `switch` expression/statement | Any | When you prefer native C# syntax |
