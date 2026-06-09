---
sidebar_position: 20
title: "PROP-020: Parameterized Business Rule Messages"
tags: [BusinessRules]
---

# PROP-020: Parameterized Business Rule Messages

**Status:** idea  
**Size:** medium  
**Created:** 2026-06-05  

## Problem / Motivation

Business rule messages are currently static strings. The `Requirement` field is a
`const string` baked in at compile time, and the only way to include runtime context
in a violation message is to call `ToException(string message)` with a manually
assembled string at the call site:

```csharp
// Today — string assembled by the caller, disconnected from the rule definition
throw OrderAmountExceeded.ToException($"Order amount {amount} exceeds maximum of {maxAmount}");
```

This breaks the relationship between the rule's declared template and the violation
message. The rule definition says nothing about what context is expected. Call sites
can format messages inconsistently. There is no structured access to the values that
caused the violation — only the final formatted string is available on the exception.

## Sketch

### JSON schema extension

Two optional fields are added to a rule definition:

```json
{
  "ClassName": "OrderAmountExceeded",
  "Key": "ORD-001",
  "Requirement": "Order amount must not exceed the maximum.",
  "Description": "Enforces the configured order ceiling.",
  "Category": "Orders",
  "MessageTemplate": "Order amount {amount} exceeds maximum of {maxAmount}.",
  "Parameters": [
    { "Name": "amount", "Type": "decimal" },
    { "Name": "maxAmount", "Type": "decimal" }
  ]
}
```

- `MessageTemplate` — a format string using named placeholders (`{name}`). Optional.
  When absent, behaviour is identical to today.
- `Parameters` — an ordered list of named, typed parameters that correspond to
  placeholders in `MessageTemplate`. Optional. Required if `MessageTemplate` is set.

Rules without `MessageTemplate` are unchanged. No breaking changes.

### Generated code

```csharp
public class OrderAmountExceeded()
    : BusinessRule<OrderAmountExceeded>(Key, Requirement, Description, Category)
{
    public const string Key = "ORD-001";
    public const string Requirement = "Order amount must not exceed the maximum.";
    public const string Description = "Enforces the configured order ceiling.";
    public const string Category = "Orders";
    public const string MessageTemplate = "Order amount {amount} exceeds maximum of {maxAmount}.";

    // Existing parameterless overload — unchanged
    public static new BusinessRuleViolationException ToException()
        => new(new OrderAmountExceeded());

    // Generated strongly-typed overload
    public static BusinessRuleViolationException ToException(decimal amount, decimal maxAmount)
    {
        var message = $"Order amount {amount} exceeds maximum of {maxAmount}.";
        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = amount,
            ["maxAmount"] = maxAmount,
        };
        return new BusinessRuleViolationException(new OrderAmountExceeded(), message, parameters);
    }
}
```

### `BusinessRuleViolationException` extension

A new constructor and property to carry structured parameters:

```csharp
public BusinessRuleViolationException(
    BusinessRuleBase businessRule,
    string message,
    IReadOnlyDictionary<string, object?> parameters)
    : base(message)
{
    BusinessRule = businessRule;
    Parameters = parameters;
}

/// <summary>
/// Named parameter values that were substituted into the violation message.
/// Empty when no template parameters were used.
/// </summary>
public IReadOnlyDictionary<string, object?> Parameters { get; } 
    = ReadOnlyDictionary<string, object?>.Empty;
```

This enables structured logging (`exception.Parameters["amount"]`) without parsing
the formatted message string.

### Separation of Requirement and MessageTemplate

`Requirement` and `MessageTemplate` serve different purposes and are kept separate:

| Field | Purpose | Example |
|-------|---------|---------|
| `Requirement` | The rule as a law — timeless, parameterless | "Order amount must not exceed the maximum." |
| `MessageTemplate` | The violation as an event — contextual, parameterized | `"Order amount {amount} exceeds maximum of {maxAmount}."` |

Merging them into one field would conflate the rule's invariant definition with its
runtime diagnostic output.

### Backward compatibility

- Rules without `MessageTemplate` are generated identically to today.
- The parameterless `ToException()` is preserved on all rules.
- Existing call sites require no changes.
- `BusinessRuleViolationException.Parameters` defaults to an empty dictionary,
  so existing code inspecting the exception is unaffected.

## Open Questions

- **Parameter types** — limit to a set of well-known types (`string`, `int`,
  `decimal`, `double`, `bool`, `Guid`, `DateTime`)? Or allow any type name
  and trust the consumer to use valid ones?
- **Analyzer coverage** — should a new diagnostic warn if `MessageTemplate` contains
  a placeholder with no matching entry in `Parameters`, or vice versa? (Validates
  the JSON definition at compile time.)
- **WCF support** — should `Parameters` be serialized onto `BusinessRules.Wcf` fault
  contracts, or is that out of scope for this proposal?
- **Nullability** — should parameter values be `object?` (flexible) or typed per
  entry? The `Dictionary<string, object?>` approach loses the type safety gained
  from the generated overload, but is simpler on the exception side.

## Prior Art / References

- [Microsoft.Extensions.Logging structured logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging#log-message-template) — named
  placeholder convention `{Name}` is established and familiar
- [FluentValidation message placeholders](https://docs.fluentvalidation.net/en/latest/custom-validators.html#writing-a-custom-validator) — similar
  pattern of named tokens in validation messages
- [Humanizer](https://github.com/Humanizr/Humanizer) — demonstrates typed
  string formatting in library contexts

## Outcome

_Filled when status changes to done/parked. Link to ADR(s) if applicable._
