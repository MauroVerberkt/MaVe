---
sidebar_position: 4
title: "ADR-004: ResultExtensions as Glue Library"
tags: [Monads, BusinessRules]
---

# ADR-004: ResultExtensions as Glue Library

**Status:** Accepted

## Context

`Monads` (Result pattern) and `BusinessRules` (rule validation) are independently useful libraries. Some consumers want both, some want only one:

- A project using Result for HTTP client calls doesn't need BusinessRules
- A project using BusinessRules with exception-based handling doesn't need Monads
- A project wanting functional validation (Result + BusinessRules together) needs both

If integration code lives in either core library, it forces an unwanted dependency on the other.

## Decision

Create `BusinessRules.ResultExtensions` as a dedicated **glue library** that depends on both `BusinessRules` and `Monads`, providing:

- `EnsureBusinessRule<T>` - predicate validation returning Result
- `ValidateAll<T>` - multiple rule validation
- `ValidateAndReturn` / `ValidateAndReturnAsync` - wraps exception-throwing code into Result
- `ToResult<T>` - converts exceptions to failed Results
- `ToBusinessRuleException<T>` - converts failed Results back to exceptions

## Consequences

**Positive:**
- Both core libraries remain independently usable with zero cross-dependencies
- Consumers only take the integration dependency when they explicitly need it
- Clear separation of concerns: each package does one thing
- Standard "glue library" pattern familiar from the .NET ecosystem (e.g., `Microsoft.Extensions.*.Abstractions`)

**Negative:**
- Additional package to discover, reference, and version
- Consumers need to know the integration package exists
- Three packages to reference for the full experience (Monads + BusinessRules + ResultExtensions)
