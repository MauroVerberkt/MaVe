---
sidebar_position: 10
title: "PROP-010: Multi-TFM Support"
tags: [infra, packaging, Monads, BusinessRules]
---

# PROP-010: Multi-TFM Support

**Status:** idea  
**Size:** small  
**Created:** 2025-05-26  

## Problem / Motivation

Currently only the analyzer/generator projects target `netstandard2.0`. The library packages (Monads, BusinessRules, etc.) target `net8.0` only. As .NET 9 and 10 ship, consumers on newer runtimes may want to use these packages without being pinned. Multi-TFM (`net8.0;net9.0;net10.0`) would widen compatibility, enable CI matrix testing across runtimes, and future-proof the packages.

## Sketch

- Add multiple TFMs to library `.csproj` files
- CI matrix build: test each TFM (ties into PROP-009)
- Evaluate whether any APIs benefit from newer runtime features via `#if` conditional compilation

## Outcome

_Filled when status changes to done/parked._
