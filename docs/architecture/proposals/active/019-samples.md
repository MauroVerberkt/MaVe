---
sidebar_position: 19
title: "PROP-019: Sample Projects"
tags: [Monads, BusinessRules, Unions, infra]
---

# PROP-019: Sample Projects

**Status:** idea  
**Size:** small  
**Created:** 2026-06-05  

## Problem / Motivation

The packages in this repository should each be independently usable. Without sample
projects, there is no canonical demonstration of intended usage, no ergonomic validation
beyond unit tests, and no living reference for how the packages compose in practice.
Samples serve as synthetic demos and dogfooding vehicles — forcing real-world API usage
patterns to surface design friction before it reaches production consumers.

## Sketch

### Project structure

```
samples/
├── Monads.Sample/              # Result<T> and Option<T> in isolation
├── BusinessRules.Sample/       # Business rule definition, validation, messages
├── Unions.Sample/              # Discriminated union declaration and matching
└── Combined.Sample/            # Full composition: rules → Result via ResultExtensions
```

Each sample is a standalone console app with no shared infrastructure.

### Scenarios per sample

**Monads.Sample**
- Happy-path Result chain: `Map().Bind().Map()`
- Error propagation: failure short-circuits the chain
- Option for nullable lookup: `Option.Some(x).Match(...)`
- Converting between Result and Option

**BusinessRules.Sample**
- Defining a business rule with the source generator
- Validating against a domain object
- Handling rule violations (message formatting)

**Unions.Sample**
- Declaring a discriminated union
- Exhaustive matching
- Analyzer feedback for non-exhaustive matches

**Combined.Sample**
- Business rule violations surfaced as `Result.Fail`
- A simple domain flow: validate input → process → return Result
- Demonstrates why `BusinessRules.ResultExtensions` exists

### Reference strategy (phased)

**Phase 1 — active development (now):**  
Use `ProjectReference`. Keeps samples in sync with source, zero publishing friction,
maximises speed of ergonomic iteration. Packaging correctness is validated separately
by the `tools/*.Package` projects and CI.

**Phase 2 — post-1.0 / API stabilisation:**  
Switch to `PackageReference` from NuGet. Validates the real consumer experience:
packaging metadata, transitive dependencies, analyzer and generator delivery via
the NuGet package.

GitHub Packages (pre-release) is explicitly skipped: at 0.x.x, requiring a package
publish before a sample compiles creates too much friction for an actively evolving API.

## Open Questions

- Should samples be included in the main solution file (`MaVe.sln`) or kept separate?
- Is a `samples/` folder at root the right convention, or `examples/`?
- Should `Combined.Sample` include `BusinessRules.Wcf` for completeness, or keep it out
  given its niche use case?

## Prior Art / References

- [dotnet/samples](https://github.com/dotnet/samples) — per-topic console apps
- [ErrorOr samples](https://github.com/amantinband/error-or/tree/main/samples) — single combined sample app
- [LanguageExt samples](https://github.com/louthy/language-ext/tree/main/Samples) — per-concept console apps

## Outcome

_Filled when status changes to done/parked. Link to ADR(s) if applicable._
