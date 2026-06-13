---
sidebar_position: 3
title: Diagnostics
description: Compile-time Roslyn diagnostics for Railyard operations — RY1001, RY1002, RY1003
keywords:
  - railyard
  - diagnostics
  - analyzer
  - RY1001
  - RY1002
  - RY1003
  - roslyn
---

# Diagnostics

The `Railyard` package includes a Roslyn source generator that validates `[Operation]`-annotated classes at compile time. Misconfigured operations produce build errors before any code runs.

## RY1001 — Duplicate Operation Name

**Severity:** Error

Fires when two or more classes declare the same operation name within the assembly.

### Example

```csharp
[Operation("greet")]
public sealed class GreetOperation : Operation<GreetInput, GreetOutput> { ... }

// RY1001: Operation name 'greet' is declared more than once
[Operation("greet")]
public sealed class GreetV2Operation : Operation<GreetInput, GreetOutput> { ... }
```

### Fix

Give each operation a unique name:

```csharp
[Operation("greet")]
public sealed class GreetOperation : Operation<GreetInput, GreetOutput> { ... }

[Operation("greet-v2")]
public sealed class GreetV2Operation : Operation<GreetInput, GreetOutput> { ... }
```

When RY1001 fires, **neither** of the conflicting operations is registered in the generated dispatch table. Both must be renamed before any generation occurs for those names.

---

## RY1002 — Invalid Operation Base Type

**Severity:** Error

Fires when `[Operation]` is applied to a class that does not inherit from `Operation<TInput, TOutput>` or `SyncOperation<TInput, TOutput>`.

### Example

```csharp
// RY1002: Operation 'greet' must inherit from Operation<TInput, TOutput>
// or SyncOperation<TInput, TOutput>
[Operation("greet")]
public sealed class GreetOperation
{
}
```

### Fix

Inherit from the correct base class:

```csharp
[Operation("greet")]
public sealed class GreetOperation : Operation<GreetInput, GreetOutput>
{
    protected override async Task<Result<GreetOutput>> ExecuteAsync(GreetInput input, CancellationToken ct)
    {
        return Result.Success(new GreetOutput($"Hello, {input.Name}!"));
    }
}
```

---

## RY1003 — Invalid Operation Name

**Severity:** Error

Fires when the operation name does not match the required pattern: `^[A-Za-z][A-Za-z0-9_-]*$`.

The name must:
- Start with a letter (`A–Z`, `a–z`)
- Contain only letters, digits, underscores (`_`), or hyphens (`-`)

### Example

```csharp
// RY1003: Operation name 'my operation' is invalid.
// Use pattern: ^[A-Za-z][A-Za-z0-9_-]*$
[Operation("my operation")]
public sealed class MyOperation : Operation<MyInput, MyOutput> { ... }
```

### Fix

Use a valid name:

```csharp
[Operation("my-operation")]
public sealed class MyOperation : Operation<MyInput, MyOutput> { ... }
```

Valid examples: `greet`, `get-user`, `send_message`, `ProcessOrder`

---

## Suppressing Diagnostics

All three diagnostics are errors and cannot be suppressed with `#pragma warning disable` in standard configurations. Fix the underlying issue rather than suppressing the diagnostic.

To change severity project-wide, add to `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.RY1001.severity = warning
dotnet_diagnostic.RY1002.severity = warning
dotnet_diagnostic.RY1003.severity = warning
```

:::warning

Downgrading these to warnings means invalid operations silently produce no entry in the dispatch table. A runtime `RY002` error at dispatch time is the result. Keeping them as errors is strongly recommended.

:::
