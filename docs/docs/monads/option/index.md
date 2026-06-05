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

### `Some<TValue>`

Represents an option that **contains a value**. Overrides `HasValue` to return `true`.

### `None<TValue>`

Represents an **empty option**. Overrides `HasValue` to return `false` and throws an exception if `Value` is accessed.

### Exceptions

- `OptionIsNoneException` - Thrown when accessing value of None
- `OptionNotPresentException` - Thrown for invalid operations on empty options

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

## Benefits

- **Null Safety**: Explicitly avoids `null` values, preventing `NullReferenceException`
- **Cleaner Code**: Self-documenting - indicates a value might be missing and forces explicit handling
- **Functional Style**: Leverages pattern matching for handling complex flows with optional values
- **Type Safety**: The compiler ensures you handle both Some and None cases

:::warning[Accessing Value Directly]

Never access `.Value` without first checking `.HasValue`. If you access `Value` on a `None`, an `OptionIsNoneException` will be thrown. Always prefer using `Match` instead.

:::

## See Also

- [Result Monad](../result/index.md) - For operations that can fail with an error
- [Monads Overview](../overview.md) - Comparison of Result vs Option
