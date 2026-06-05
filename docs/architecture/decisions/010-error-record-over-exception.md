---
sidebar_position: 10
title: "ADR-010: Error Record Over Exception"
tags: [Monads]
---

# ADR-010: Error Record Over Exception

**Status:** Accepted

## Context

`Result<TData>` uses `Exception?` as the error type for failure states. While this was a pragmatic starting point (exceptions are universally understood in .NET), it creates several problems:

- **Heavyweight allocation**: Exceptions capture stack traces even when never thrown, wasting resources for expected domain errors (not found, validation failures)
- **Poor equality semantics**: `Exception` uses reference equality, so two identical failures aren't equal — breaking value-type expectations on `Result<TData>.Equals()`
- **Awkward testing**: Tests must construct exception objects for simple failure scenarios (`new InvalidOperationException("Not found")`)
- **Serialization**: Exceptions serialize poorly (stack traces, internal type names leak across boundaries)
- **Impedance mismatch**: `BusinessRules.ResultExtensions` wraps rule violations into exceptions just to satisfy the `Result<TData>` API, then consumers downcast back out

## Decision

Replace `Exception?` with a lightweight `Error` record:

```csharp
public record Error(string Message, string? Code = null, Exception? Exception = null)
{
    public static Error Create(string message, string? code = null) => new(message, code);
    public static Error Unexpected(Exception exception) => new(exception.Message, "UNEXPECTED", exception);
}
```

Design choices:

1. **Record type** — value equality, immutability, `with` expressions, clean `ToString()`
2. **Minimal factory methods** — only `Create` (explicit domain error) and `Unexpected` (caught exception). No opinionated "common errors" (NotFound, Validation) in the library; consumers define their own via static classes.
3. **`Code` is `string?`** — optional machine-readable identifier. Not an enum (avoids opinionated vocabulary). Consumers who need typed codes can define their own conventions.
4. **`Exception?` on the base** — preserves original exception at boundaries for logging/diagnostics without requiring downcasting. Usually `null` for domain errors.
5. **Single error per Result** — no built-in error list. Error aggregation (e.g., multiple validation failures) is a separate concern (see PROP-013).
6. **Extensibility via consumer static classes** — consumers add domain errors without inheriting from `Error` or implementing interfaces.

## Consequences

**Positive:**

- Testing is natural: `Error.Create("User not found", "NOT_FOUND")` instead of constructing exception objects
- Value equality works: two `Error.Create("x", "Y")` instances are equal
- No stack trace overhead for domain errors
- Clean serialization (records serialize naturally as JSON)
- `BusinessRules.ResultExtensions` becomes cleaner — wraps at the boundary with `Error.Unexpected(ex)`
- Extension story is trivial — consumers just write `public static class OrderErrors { ... }`

**Negative:**

- Breaking change for all consumers of `Result<TData>.Error` (previously `Exception`, now `Error`)
- Consumers using `OnFailure(Action<Exception>)` must update to `Action<Error>`
- Code that passes `result.Error` directly to `ILogger.LogError(Exception, ...)` must access `result.Error.Exception` instead
- Acceptable within 0.x semver

**Migration path for consumers:**

- `Result.Failure<T>(new SomeException("msg"))` → `Result.Failure<T>(Error.Create("msg"))` or `Result.Failure<T>(Error.Unexpected(ex))` at boundaries
- `result.Error.Message` → unchanged (same property name)
- `result.Error as SpecificException` → `result.Error.Exception as SpecificException`
