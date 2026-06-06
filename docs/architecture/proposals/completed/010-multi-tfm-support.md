---
sidebar_position: 10
title: "PROP-010: Multi-TFM Support"
tags: [infra, packaging, Monads, BusinessRules]
---

# PROP-010: Multi-TFM Support

**Status:** done  
**Size:** small  
**Created:** 2025-05-26  

## Problem / Motivation

Currently only the analyzer/generator projects target `netstandard2.0`. The library packages (Monads, BusinessRules, etc.) target `net8.0` only. As .NET 9 and 10 ship, consumers on newer runtimes may want to use these packages without being pinned. Multi-TFM (`net8.0;net9.0;net10.0`) would widen compatibility, enable CI matrix testing across runtimes, and future-proof the packages.

## Sketch

- Add multiple TFMs to library `.csproj` files
- CI matrix build: test each TFM (ties into PROP-009)
- Evaluate whether any APIs benefit from newer runtime features via `#if` conditional compilation

## Outcome

Implemented June 2026 (commit `fdad7b1`, PR #18).

All library and test projects now target `net8.0;net9.0`:

- `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>` added to all packable projects (`Monads`, `BusinessRules`, `BusinessRules.ResultExtensions`, `BusinessRules.Wcf`, `Unions`) and their corresponding test projects.
- `_build-and-test.yml` updated to install both the .NET 8 and .NET 9 SDKs so both TFMs are built and tested on every CI run.
- `fetch-depth: 0` (full clone) added to CI as a side-effect of the NBGV integration (PROP-011) — required for NBGV height calculation.
- No `#if` conditional compilation was needed: the API surface is identical across both TFMs.
