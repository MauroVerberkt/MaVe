---
sidebar_position: 0
title: Monads Overview
description: Functional programming patterns for C# - Result and Option types
keywords:
  - monads
  - functional programming
  - result
  - option
  - railway oriented programming
---

# Monads Overview

MaVe provides two core monadic types for safer, more expressive C# code:

## Result&lt;T&gt;

Represents the outcome of an operation that can either **succeed with data** or **fail with an error**.

```csharp
Result<User> result = GetUser(42);

if (result.IsSuccess)
    Console.WriteLine(result.Data.Name);
else
    Console.WriteLine(result.Error.Message);
```

**Use when:**
- Operations can fail in expected ways (validation, not found, etc.)
- You want to avoid exceptions for control flow
- You need to chain multiple operations that might fail

[Learn more about Result&lt;T&gt;](./result/index.md)

## Option&lt;T&gt;

Represents a value that **may or may not be present**, eliminating null references.

```csharp
Option<string> name = Option<string>.Some("John");

string greeting = name.Match(
    some: n => $"Hello, {n}!",
    none: () => "Hello, stranger!"
);
```

**Use when:**
- A value is genuinely optional (not an error case)
- You want to eliminate `NullReferenceException`
- You prefer explicit handling of absent values over null checks

[Learn more about Option&lt;T&gt;](./option/index.md)

## Comparison

| Scenario | Use |
|----------|-----|
| Operation might fail with an error | `Result<T>` |
| Value might not exist (no error) | `Option<T>` |
| Need to chain fallible operations | `Result<T>` with `Then`/`Bind`/`Map` |
| Need to transform optional values | `Option<T>` with `Map`/`Bind` |
| Need exhaustive matching on outcome | `Result<T>.Match` or `Option<T>.Match` |
| Prefer LINQ query syntax | Both — `Select`/`SelectMany` on both types |

:::tip[C# to Functional Mapping]

If you're familiar with other languages:
- `Result<T>` is similar to Rust's `Result<T, E>` or F#'s `Result<'T, 'TError>`
- `Option<T>` is similar to Rust's `Option<T>`, F#'s `Option<'T>`, or Java's `Optional<T>`

:::
