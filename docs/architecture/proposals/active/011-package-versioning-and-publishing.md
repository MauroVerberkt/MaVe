---
sidebar_position: 11
title: "PROP-011: Package Versioning and Publishing"
tags: [infra, packaging, Monads, BusinessRules]
---

# PROP-011: Package Versioning and Publishing

**Status:** exploring  
**Size:** medium  
**Created:** 2026-05-27  

## Problem / Motivation

The repo produces multiple independently-consumable NuGet packages (`Monads`, `BusinessRules`, analyzers, extension bridges) but has no versioning or publishing strategy. Before packages can be shipped to consumers we need:

1. **Deterministic, reproducible versions** — any commit should produce a traceable version.
2. **Independent versioning per package** — a change to `Monads` shouldn't force a version bump in `BusinessRules`.
3. **Automatic patch increments** — avoid manual version bookkeeping for every merge.
4. **Prerelease support** — feature branches produce prerelease packages automatically.
5. **A publishing pipeline** — CI builds, packs, and pushes only new versions.

## Sketch

### Versioning: Nerdbank.GitVersioning (NBGV)

Each publishable package gets its own `version.json` with path filters so that git height is scoped to its own source:

```
src/
├── Monads/
│   └── version.json          → "version": "0.1"
├── BusinessRules/
│   └── version.json          → "version": "0.1"
├── BusinessRules.ResultExtensions/
│   └── version.json          → "version": "0.1"
└── BusinessRules.Wcf/
    └── version.json          → "version": "0.1"
```

Example `version.json`:

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/main/src/NerdBank.GitVersioning/version.schema.json",
  "version": "0.1",
  "pathFilters": ["."],
  "publicReleaseRefSpec": [
    "^refs/heads/main$"
  ],
  "cloudBuild": {
    "setVersionVariables": true,
    "buildNumber": {
      "enabled": true
    }
  }
}
```

**Version semantics:**
- Major.Minor — set manually in `version.json` when introducing features (minor) or breaking changes (major).
- Patch — git height (commit count since last `version.json` change for that path). Automatic, monotonically increasing.
- Prerelease suffix — added automatically for non-main branches.

### Merge Strategy

**Squash merge only** (already enforced in GitHub repo settings).

This ensures 1 PR = 1 patch increment — predictable and clean.

### Cross-Package Dependencies

During development: `ProjectReference` (unchanged — fast inner loop, single build graph).

At pack time: NuGet automatically converts project references to package dependencies using the version NBGV calculated for the referenced project. The resulting `.nupkg` declares minimum-version dependencies (e.g., `Monads >= 1.0.23`).

When a dependency package makes a **breaking change**, the dependent package must update its code, which naturally creates a commit in its path and bumps its own version.

### Analyzer/Generator Packaging

The analyzer packages (`BusinessRules.Analyzers.Package`) bundle the analyzer, generator, and fix provider into a single NuGet package. This package gets its own `version.json` and tracks changes across all three source projects via path filters:

```jsonc
// tools/BusinessRules.Analyzers.Package/version.json
{
  "version": "0.1",
  "pathFilters": [
    ".",
    "../../src/BusinessRulesAnalyzer",
    "../../src/BusinessRulesGenerator",
    "../../src/BusinessRulesFixProvider"
  ]
}
```

### Debuggability: SourceLink & Symbols

Consumers should be able to step into package source code during debugging. This requires **SourceLink** (maps PDB info to GitHub source) and **symbol packages** (`.snupkg` published alongside the main package).

Configuration in `src/Directory.Build.props` (scoped to packable projects):

```xml
<PropertyGroup Condition="'$(IsPackable)' == 'true'">
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>

<ItemGroup Condition="'$(IsPackable)' == 'true'">
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

**Why each property:**

| Property | Purpose |
|----------|---------|
| `PublishRepositoryUrl` | Embeds the `RepositoryUrl` in package metadata (already set in root props) |
| `EmbedUntrackedSources` | Includes source-generated files (not in git) in the PDB — critical for our Roslyn generators |
| `IncludeSymbols` | Produces a `.snupkg` alongside the `.nupkg` |
| `SymbolPackageFormat` | Uses the modern `snupkg` format (supported by NuGet.org and GitHub Packages) |

**Deterministic builds & CI:**

- `<Deterministic>true</Deterministic>` is already the .NET SDK default — file paths in PDBs are normalized.
- `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>` must be set in CI to fully normalize paths. NBGV sets this automatically in cloud builds, so no additional configuration is needed.

**Pipeline impact:** The `dotnet nuget push` step pushes both `.nupkg` and `.snupkg` in a single command — no separate symbol server configuration required.

### Publishing Pipeline (CI)

```
trigger: push to main

steps:
  1. dotnet restore
  2. dotnet build (NBGV sets versions automatically)
  3. dotnet test
  4. dotnet pack (all publishable projects)
  5. For each .nupkg:
     - Check if this version already exists on NuGet feed
     - If new → dotnet nuget push
     - If exists → skip (package unchanged)
```

Unchanged packages produce the same version as last publish → NuGet rejects duplicates → safe to "push all, skip duplicates".

### NuGet Feed Target

**GitHub Packages** (sole target for now). Public nuget.org publishing may come later but is not in scope.

### Prerelease Strategy

Handled automatically by NBGV:
- Builds from `main` → stable versions (e.g., `0.1.23`)
- Builds from any other branch → prerelease (e.g., `0.1.23-beta.gabcdef1`)
- Prerelease packages are published on-demand only (not on every PR build)

## Open Questions

- ~~Should prerelease packages be published automatically on every PR build, or only on-demand?~~ **On-demand only.**
- ~~Should the analyzers package version track independently or stay pinned to the `BusinessRules` core package major version?~~ **Track BusinessRules major version (shared epoch), independent minor/patch.** The analyzer enforces conventions of the library, so consumers can rely on "analyzer 1.x works with BusinessRules 1.x".
- ~~Do we need a CHANGELOG per package, or is git history + release notes sufficient?~~ **GitHub Releases with auto-generated notes.** Tag per package (e.g., `helpermonads-v1.0.23`). No per-package CHANGELOG file — not justified at this scale.
- ~~What's the initial version for each package — `1.0` (signals stability) or `0.x` (signals pre-stable)?~~ **`0.x`** — signals pre-stable, allows breaking changes without major bumps per SemVer conventions.

## Prior Art / References

- [Nerdbank.GitVersioning docs](https://github.com/dotnet/Nerdbank.GitVersioning/blob/main/doc/versionJson.md)
- [NBGV path filters](https://github.com/dotnet/Nerdbank.GitVersioning/blob/main/doc/pathFilters.md)
- [Azure SDK for .NET versioning](https://github.com/Azure/azure-sdk-for-net/blob/main/doc/DataPlaneCodeGeneration/AzureSDKCodeGeneration_DataPlane.mdc) — independent per-package versioning in a monorepo
- [NuGet duplicate push behavior](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#package-validation-and-indexing) — HTTP 409 on duplicate, safe to skip

## Outcome

_Filled when status changes to done/parked. Link to ADR(s) if applicable._
