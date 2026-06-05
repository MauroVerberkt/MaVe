---
sidebar_position: 12
title: "PROP-012: Public API Tracking"
tags: [infra, packaging, Monads, BusinessRules]
---

# PROP-012: Public API Tracking

**Status:** exploring  
**Size:** small  
**Created:** 2026-05-27  

## Problem / Motivation

The repo produces multiple NuGet packages consumed by other projects. Accidental breaking changes — removing a method, changing a signature, exposing an internal type — are easy to introduce without noticing, especially in a single-maintainer project without dedicated API review.

We need a mechanism that:

1. Makes the public API surface **explicit and version-controlled**
2. **Fails the build** when public API changes are unintentional
3. Provides a clear "acknowledge this change" workflow for intentional additions
4. Distinguishes between **shipped** (released) and **unshipped** (upcoming) API surface

## Sketch

### Tool: Microsoft.CodeAnalysis.PublicApiAnalyzers

Roslyn analyzer maintained by Microsoft (used in dotnet/runtime, Roslyn, ASP.NET Core). Enforces public API tracking at compile time via two text files per project:

- `PublicAPI.Shipped.txt` — APIs that have been released in a stable package
- `PublicAPI.Unshipped.txt` — APIs added since the last release (pending shipment)

Any public member not listed in either file produces a compiler warning. With `TreatWarningsAsErrors` (already enabled repo-wide), this becomes a build error.

### Setup

Add to `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="3.11.0" />
```

Add to `src/Directory.Build.props` (scoped to packable projects):

```xml
<ItemGroup Condition="'$(IsPackable)' == 'true'">
  <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" PrivateAssets="All" />
</ItemGroup>
```

Each packable project gets:

```
src/Monads/
├── PublicAPI.Shipped.txt
├── PublicAPI.Unshipped.txt
├── Result.cs
└── ...
```

### Initial State (Pre-1.0)

Since all packages are `0.x` (pre-stable per PROP-011), nothing has been "shipped" yet:

- `PublicAPI.Shipped.txt` — empty (contains only `#nullable enable` header)
- `PublicAPI.Unshipped.txt` — all current public APIs

This is correct semantically: nothing is locked down yet, everything is subject to change.

### Release Workflow

When a package reaches `1.0` (or any version you consider stable):

1. Move contents of `Unshipped.txt` → `Shipped.txt`
2. Clear `Unshipped.txt`
3. From this point forward, removing or modifying anything in `Shipped.txt` is a breaking change (build error)

For subsequent releases: move newly accumulated `Unshipped` entries to `Shipped` as part of the release commit.

### Day-to-Day Workflow

1. **Add a new public method** → Build fails (not in either file)
2. **Run the RS0016 code fix** → IDE auto-adds the member to `Unshipped.txt`
3. **PR shows the diff** → Reviewer (or you) sees exactly what public API surface is changing
4. **Accidentally make an internal class public** → Build fails → you notice immediately

### Source Generators

The analyzer sees the final compilation including source-generated code. Generated public APIs (e.g., from `BusinessRulesGenerator`) will also need to be listed. Two approaches:

1. **List generated APIs in the txt files** — Simple, but generated APIs create noise and churn if the generator output changes shape.
2. **Suppress RS0016 for generated files** — Via `.editorconfig` scoped to generated output. Acceptable if the generator's output shape is covered by integration tests.

Recommendation: Start with option 1 (explicit listing). If noise becomes a problem, switch to option 2 with test coverage as the safety net.

### File Format Example

```text
#nullable enable
MaVe.Monads.Result<T>
MaVe.Monads.Result<T>.IsSuccess.get -> bool
MaVe.Monads.Result<T>.IsFailure.get -> bool
MaVe.Monads.Result<T>.Map<U>(System.Func<T!, U!>!) -> MaVe.Monads.Result<U!>
MaVe.Monads.Result<T>.Bind<U>(System.Func<T!, MaVe.Monads.Result<U!>!>!) -> MaVe.Monads.Result<U!>
MaVe.Monads.Result<T>.Match<U>(System.Func<T!, U!>!, System.Func<string!, U!>!) -> U!
```

### Relationship to PROP-011

This complements the versioning strategy:

- NBGV handles **version numbers** (when to bump major/minor/patch)
- PublicApiAnalyzer handles **API surface awareness** (what changed between versions)
- Together: you know *when* a version bumps and *what* the bump contains

## Open Questions

- ~~Should the analyzer be applied to non-packable projects (e.g., the analyzer/generator assemblies themselves)?~~ **No.** They have no public consumers.
- ~~How to handle the `BusinessRules.Analyzers.Package` project which bundles multiple assemblies — track APIs in each source project individually?~~ **Not applicable.** Analyzer/generator assemblies are loaded by the compiler, not referenced by consumer code. They live in the `analyzers/` folder of the NuGet package — their types are never visible to consumers. Their "public API" is the diagnostics they report and the source they generate, neither of which is tracked by `PublicAPI.txt`. The `IsPackable` scoping already excludes them correctly.

## Prior Art / References

- [Microsoft.CodeAnalysis.PublicApiAnalyzers (GitHub)](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)
- [Diagnostic RS0016: Add public types and members to the declared API](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md)
- [How dotnet/runtime uses PublicApiAnalyzers](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/api-guidelines.md)

## Outcome

_Filled when status changes to done/parked. Link to ADR(s) if applicable._
