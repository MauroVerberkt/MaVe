---
sidebar_position: 0
title: Railyard Overview
description: Compile-time generated operation dispatch for JSON payload boundaries
keywords:
  - railyard
  - source generator
  - operation dispatch
  - railway-oriented programming
  - compile-time
---

# Railyard Overview

**Railyard** provides compile-time generated operation dispatch for JSON payload boundaries. You declare a named operation class — the generator produces the dispatch table, DI registration, and manifest automatically.

## The Problem It Solves

A recurring pattern at serialization boundaries: something external sends a name and a JSON payload, and you need to route it to typed, validated code. The interesting logic for a new operation is typically 10 lines. The plumbing — registration, deserialization, routing, error translation — is 50.

Railyard eliminates the plumbing. One class with a declared name, input shape, and execute method. Everything else is generated at compile time.

## Core Concepts

| Concept | Role |
|---------|------|
| **Operation** | A named unit of work. Declares its dispatch name, typed input, typed output, and an optional validate step. |
| **Yard** | The generated dispatch registry. Maps operation names to handlers and exposes the manifest of registered operations. |
| **Manifest** | Auto-generated metadata listing all operations by name and description. Useful for tool definitions, help text, or API docs. |

## When to Use Railyard

Railyard is designed for **external serialization boundaries** — places where a caller sends a string name and a JSON payload:

- **Tool orchestrators** — route MCP tool calls to typed implementations
- **Hardware gateways** — dispatch TCP/serial messages to device operation handlers
- **Background job processors** — route queue messages by type without a full messaging framework
- **CLI applications** — dispatch subcommand payloads to handlers

## When Not to Use Railyard

- **In-process decoupling** — if the caller is typed C# code, inject the operation directly and call it with typed inputs. The `IYard.DispatchAsync` boundary is for external callers.
- **Full messaging scenarios** — Railyard has no retries, dead-letter queues, or delivery guarantees. It dispatches; transport is out of scope.
- **HTTP routing** — Railyard has no ASP.NET middleware integration. It sits behind whatever transport layer you use.

## Key Properties

- **Compile-time dispatch** — no reflection at runtime; the routing table is generated code
- **Single package** — `dotnet add package MaVe.Railyard` installs the runtime, source generator, and compile-time diagnostics
- **Async-first** — primary pipeline is `Task<Result<TOutput>>`; a synchronous convenience base class is provided
- **Result-based pipeline** — every step returns `Result<T>`; errors short-circuit without exceptions crossing the boundary
- **Scoped dispatch** — each `DispatchAsync` call gets its own DI scope; operation dependencies do not bleed across calls

## See Also

- [Getting Started](./getting-started.md) — install, define an operation, dispatch
- [Pipeline](./pipeline.md) — pipeline stages, error codes, serialization options
- [Diagnostics](./diagnostics.md) — compile-time diagnostics RY1001–RY1003
