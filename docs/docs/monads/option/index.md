---
sidebar_position: 1
title: Option Monad
description: Safely represent values that may or may not be present without null references
keywords:
  - option
  - monad
  - null safety
  - some
  - none
  - pattern matching
  - async
  - functional programming
---

# Option&lt;T&gt; Monad

The **Option Monad** is a functional programming construct used to represent a value that may or may not be present. It handles cases where a value might be missing without relying on `null`, which can lead to `NullReferenceException`.

## What is an Option Monad?

The Option Monad encapsulates a value that may or may not exist. It is used to:

- Avoid `null` references
- Make code safer and more predictable
- Explicitly handle cases where a value may be missing

In this implementation, there are two main states:

- **Some**: The option contains a value
- **None**: The option does not contain a value

## Components

### `Option<TValue>`

The abstract base class with these key features:

| Member | Description |
|--------|-------------|
| `HasValue` | Indicates whether the option contains a value |
| `Value` | The value inside the option (only accessible if `HasValue` is true) |
| `Match<TResult>` | Apply a function based on whether a value is present or not |
| `MatchAsync<TResult>` | Async version of `Match` (with `CancellationToken` overload) |
| `Map<TNew>` | Transforms the value if present, returning a new `Option<TNew>` |
| `MapAsync<TNew>` | Async version of `Map` (with `CancellationToken` overload) |
| `Bind<TNew>` | Chains with another operation returning `Option<TNew>` |
| `BindAsync<TNew>` | Async version of `Bind` (with `CancellationToken` overload) |
| `Select<TNew>` | Transforms the value (enables LINQ `select`) |
| `SelectMany<TNew, TResult>` | Chains options (enables LINQ `from … from`) |
| `operator ==` / `!=` | Value equality |

### `Some<TValue>`

Represents an option that **contains a value**. Overrides `HasValue` to return `true`.

### `None<TValue>`

Represents an **empty option**. Overrides `HasValue` to return `false` and throws `OptionIsNoneException` if `Value` is accessed.

### Exceptions

- `OptionIsNoneException` — Thrown when accessing `Value` on a `None`, or when an invalid state is reached in a chain

## Use Cases

The Option Monad is ideal for:

- Optional parameters that may or may not be provided
- Results of database queries that may return null
- Any scenario where a value can be optionally present
- Replacing null checks with explicit handling

## Example Usage

```csharp title="OptionBasics.cs"
using MaVe.Monads.Option;

// Using Some to represent a value
Option<string> someOption = Option<string>.Some("Hello, world!");

string result1 = someOption.Match(
    some: value => $"Value is: {value}",
    none: () => "No value present"
);
Console.WriteLine(result1); // Output: Value is: Hello, world!

// Using None to represent absence
Option<string> noneOption = Option<string>.None;

string result2 = noneOption.Match(
    some: value => $"Value is: {value}",
    none: () => "No value present"
);
Console.WriteLine(result2); // Output: No value present
```

### Creating Options from Nullable Values

```csharp title="FromNullable.cs"
// Using FromNullable to convert nullable types
string? nullableValue = null;
Option<string> option = Option<string>.FromNullable(nullableValue);

string result = option.Match(
    some: value => $"Value is: {value}",
    none: () => "No value present"
);
Console.WriteLine(result); // Output: No value present
```

### Implicit Conversion

```csharp title="ImplicitConversion.cs"
// Implicit conversion from nullable types
string? nullableValue = null;
Option<string> option = nullableValue;

string result = option.Match(
    some: value => $"Value is: {value}",
    none: () => "No value present"
);
Console.WriteLine(result); // Output: No value present
```

## Pattern Matching Summary

The `Match` method is the primary way to work with Option values:

```csharp title="MatchExamples.cs"
Option<int> maybeAge = GetUserAge(userId);

// Transform to a different type
string message = maybeAge.Match(
    some: age => $"User is {age} years old",
    none: () => "Age unknown"
);

// Use with side effects
maybeAge.Match(
    some: age => { _logger.LogInfo($"Age: {age}"); return true; },
    none: () => { _logger.LogWarning("No age found"); return false; }
);
```

### Async Pattern Matching

When your Some/None handlers need to perform async operations (database lookups, HTTP calls), use `MatchAsync`:

```csharp title="MatchAsyncExamples.cs"
Option<int> maybeUserId = GetCurrentUserId();

// Basic async match
UserProfile profile = await maybeUserId.MatchAsync(
    some: async id => await _userService.GetProfileAsync(id),
    none: () => Task.FromResult(UserProfile.Anonymous)
);

// With CancellationToken support
UserProfile profile = await maybeUserId.MatchAsync(
    some: async (id, ct) => await _userService.GetProfileAsync(id, ct),
    none: (ct) => Task.FromResult(UserProfile.Anonymous),
    cancellationToken
);
```

## Map — Transform Without Unwrapping

`Map` applies a function to the value inside the option, returning a new option. If the option is `None`, `Map` returns `None` without calling the function:

```csharp title="MapExample.cs"
Option<string> maybeName = GetUserName(userId);

// Transform the value if present
Option<int> maybeLength = maybeName.Map(name => name.Length);

// Chain maps
Option<string> maybeUpper = maybeName
    .Map(name => name.Trim())
    .Map(name => name.ToUpperInvariant());
```

Use `MapAsync` for async transformations:

```csharp title="MapAsyncExample.cs"
Option<int> maybeUserId = GetCurrentUserId();

Option<UserProfile> maybeProfile = await maybeUserId
    .MapAsync(async id => await _userService.GetProfileAsync(id));
```

## Bind — Chaining Optional Operations

`Bind` is for chaining operations that themselves return an `Option`. Where `Map` wraps the result automatically, `Bind` expects the function to return an `Option<TNew>` directly — preventing double-wrapping:

```csharp title="BindExample.cs"
Option<User> maybeUser = FindUser(userId);

// GetPrimaryAddress returns Option<Address> — use Bind, not Map
Option<Address> maybeAddress = maybeUser
    .Bind(user => GetPrimaryAddress(user.Id));

// Chain multiple optional lookups
Option<string> maybeCity = FindUser(userId)
    .Bind(user => GetPrimaryAddress(user.Id))
    .Bind(address => GetCity(address.PostalCode));
```

## LINQ Query Syntax

`Select` and `SelectMany` enable LINQ query syntax, which can read more naturally for multi-step pipelines:

```csharp title="LinqOptionExample.cs"
var maybeCity =
    from user in FindUser(userId)
    from address in GetPrimaryAddress(user.Id)
    select address.City;
```

This is equivalent to:

```csharp
var maybeCity = FindUser(userId)
    .Bind(user => GetPrimaryAddress(user.Id)
        .Map(address => address.City));
```

## Benefits

- **Null Safety**: Explicitly avoids `null` values, preventing `NullReferenceException`
- **Cleaner Code**: Self-documenting — indicates a value might be missing and forces explicit handling
- **Functional Style**: Leverages pattern matching for handling complex flows with optional values
- **Type Safety**: The compiler ensures you handle both Some and None cases

:::warning[Accessing Value Directly]

Never access `.Value` without first checking `.HasValue`. If you access `Value` on a `None`, an `OptionIsNoneException` will be thrown. Always prefer using `Match`, `Map`, or `Bind` instead.

:::

## See Also

- [Result Monad](../result/index.md) - For operations that can fail with an error
- [Monads Overview](../overview.md) - Comparison of Result vs Option
