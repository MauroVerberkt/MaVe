---
sidebar_position: 3
title: Analyzers
description: Compile-time diagnostics for union types - DNHU0001 and DNHU0003
keywords:
  - analyzer
  - roslyn
  - DNHU0001
  - DNHU0003
  - exhaustive
  - diagnostic
---

# Analyzers

The `HelperUnions` package includes two Roslyn analyzers that catch union-related problems at compile time.

## DNHU0001 — Non-Exhaustive Union Match

**Severity:** Warning (configurable to Error)

Fires when a `switch` statement or expression operates on a union type but does not handle all variants, and has no default or discard arm.

### Example

```csharp
// DNHU0001: Non-exhaustive union match. Missing variants: Supplier, Partner, Prospect.
var name = party switch
{
    BusinessParty.Customer { Info: var info } => info.Name,
};
```

### Fix

Add the missing arms (use the "Add missing union variant arms" code fix), or add a discard arm to opt out of exhaustiveness checking:

```csharp
var name = party switch
{
    BusinessParty.Customer { Info: var info } => info.Name,
    BusinessParty.Supplier { Info: var info } => info.CompanyName,
    BusinessParty.Partner { Info: var info, Tier: var tier } => $"{info.Name} ({tier})",
    BusinessParty.Prospect => "Unknown",
};
```

### Code Fix

The "Add missing union variant arms" code fix is available for both switch expressions and switch statements. It inserts `throw new System.NotImplementedException()` stubs for each missing variant so you can fill them in immediately.

### Configuring Severity

To promote this to an error in your project, add to `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.DNHU0001.severity = error
```

### Note on Builder Chains

The `Match().Result()` and `Switch().Execute()` builder chains do not need DNHU0001 — exhaustiveness is enforced directly by the type system. DNHU0001 applies only to native C# `switch` statements and expressions.

---

## DNHU0003 — Invalid Union Declaration

**Severity:** Error

Fires when `[Union]` is applied to a type that is not a `partial record`.

### Example

```csharp
// DNHU0003: [Union] may only be applied to partial record declarations.
[Union]
public partial class BusinessParty { }

// DNHU0003: [Union] may only be applied to partial record declarations.
[Union]
public record BusinessParty { }  // missing 'partial'
```

### Fix

Change the declaration to `partial record`:

```csharp
[Union]
public partial record BusinessParty
{
    public sealed record Customer(CustomerInfo Info) : BusinessParty;
    // ...
}
```

---

## Analyzer Release Notes

Both rules are listed in `AnalyzerReleases.Unshipped.md` in the `HelperUnionsAnalyzer` project, as required by the `RS2008` Roslyn analyzer packaging convention.
