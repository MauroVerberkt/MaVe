---
sidebar_position: 18
title: "PROP-018: Benchmarks"
tags: [Monads, infra]
---

# PROP-018: Benchmarks

**Status:** idea  
**Size:** small  
**Created:** 2026-06-05  

## Problem / Motivation

The core value proposition of the Monads package is that `Result<T>`/`Option<T>` provide
safer error handling *without meaningful performance cost* compared to exceptions and
null checks. Without benchmarks, this claim is anecdotal. Hard numbers strengthen the
case when adopting these packages in production codebases and help detect performance
regressions as the API surface grows.

## Sketch

### Project structure

```
benchmarks/
├── Monads.Benchmarks/          # Result<T>, Option<T> vs. alternatives
└── (future: BusinessRules.Benchmarks/)
```

### Scenarios (Monads, first pass)

| Benchmark | Baseline | Subject |
|-----------|----------|---------|
| Happy path return | `return value` | `Result.Ok(value)` |
| Error path | `throw new Exception(...)` | `Result.Fail(error)` |
| Nullable check | `if (x != null)` | `Option.Some(x).Match(...)` |
| Chain (success) | nested try/catch | `Map().Bind().Map()` |
| Chain (failure at step N) | exception unwind | `Bind()` short-circuit |
| Pattern match | switch on type | `result.Match(ok, fail)` |

### Tooling

- BenchmarkDotNet (latest stable)
- `[MemoryDiagnoser]` on all benchmarks for allocation tracking
- Export results as markdown for easy inclusion in docs

### CI integration (future)

Not day-one scope. Eventually:
- Run benchmarks on PR (dedicated job, allow-failure)
- Store results as build artifacts
- Consider regression thresholds via baseline comparison

## Open Questions

- Include BusinessRules benchmarks in the initial pass, or Monads-only first?
- Should benchmark results be published to the docs site (e.g., a "Performance" page)?
- Target framework for benchmarks: `net8.0` only, or multi-TFM to show perf across runtimes?

## Prior Art / References

- [BenchmarkDotNet docs](https://benchmarkdotnet.org/)
- [ErrorOr benchmarks](https://github.com/amantinband/error-or) — similar Result-type library with published perf numbers
- [LanguageExt benchmarks](https://github.com/louthy/language-ext) — functional C# library

## Outcome

_Filled when status changes to done/parked. Link to ADR(s) if applicable._
