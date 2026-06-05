---
sidebar_position: 1
title: "ADR-001: Result Pattern Over Exceptions"
tags: [Monads]
---

# ADR-001: Result Pattern Over Exceptions

**Status:** Accepted

## Context

In traditional C# code, errors are communicated through exceptions. However, when exceptions are used for expected failure cases (validation failures, not-found scenarios, business rule violations), they become a control flow mechanism rather than an exceptional circumstance. This leads to:

- Hidden control flow that isn't visible in method signatures
- Performance overhead from stack unwinding
- Difficulty composing multiple fallible operations
- `try/catch` blocks scattered throughout business logic

## Decision

Implement a `Result<TData>` type that explicitly represents success or failure in the type system. Operations that can fail return `Result<T>` instead of throwing exceptions. The type provides functional composition through `Map`, `Bind`, and side-effect methods (`OnSuccess`, `OnFailure`, `Tap`).

Exceptions are reserved for truly exceptional, unexpected situations (programmer errors, infrastructure failures).

## Consequences

**Positive:**
- Error paths are visible in method signatures (`Result<User>` instead of `User`)
- No performance cost from stack unwinding on expected failures
- Operations compose naturally with `Map`/`Bind` (railway-oriented programming)
- Compiler enforces handling via `[MemberNotNullWhen]` attributes - no null warnings when branching on `IsSuccess`
- Eliminates defensive `try/catch` in business logic

**Negative:**
- Unfamiliar pattern for developers coming from exception-heavy C# codebases
- Slight verbosity increase for simple operations
- Interop with exception-throwing code requires wrapping (addressed by `BusinessRules.ResultExtensions`)
- Cannot use `using` statements or `await using` directly inside Result chains
