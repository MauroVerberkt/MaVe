---
sidebar_position: 4
title: Build and Packaging
---

# Build and Packaging

## `Directory.Build.props` Hierarchy

MSBuild automatically imports `Directory.Build.props` files from parent
directories. This repo uses three layers:

```
MaVe/
├── Directory.Build.props        ← root: shared metadata + analyzers
├── src/
│   └── Directory.Build.props    ← src: docs, SourceLink, PublicApiAnalyzers
└── tests/
    └── Directory.Build.props    ← tests: test framework references
```

Each layer imports its parent before adding its own properties.

### Root `Directory.Build.props`

Applied to every project in the repo:

- Package metadata: `Authors`, `Copyright`, `RepositoryUrl`, `PackageLicenseExpression`
- Language: `LangVersion=latest`, `ImplicitUsings=enable`, `Nullable=enable`
- Analyzers: `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`
- Code style: `EnforceCodeStyleInBuild=true` (applies `.editorconfig` rules at build time)
- Warnings: `TreatWarningsAsErrors=true` in Release configuration only
- Packages: `Nerdbank.GitVersioning` (versioning), `Roslynator.Analyzers` (extended analysis)

### `src/Directory.Build.props`

Applied to all projects under `src/`:

- `GenerateDocumentationFile=true` — XML doc generation for all src projects
- Packable non-analyzer projects additionally get:
  - `PackageReadmeFile`, `PublishRepositoryUrl`, `EmbedUntrackedSources`
  - `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`
  - `Microsoft.SourceLink.GitHub` — embeds source links into symbols
  - `Microsoft.CodeAnalysis.PublicApiAnalyzers` (net8.0 TFM only, to avoid cross-TFM API file divergence)
- Roslyn component projects (`IsRoslynComponent=true`) get an additional
  `roslyn-components.globalconfig` suppression overlay — see below

### `tests/Directory.Build.props`

Applied to all projects under `tests/`:

- `IsPackable=false`, `IsTestProject=true`
- `TargetFrameworks=net8.0;net9.0` — tests run on both TFMs
- Test framework packages: `NUnit`, `NUnit.Analyzers`, `NUnit3TestAdapter`,
  `Microsoft.NET.Test.Sdk`, `coverlet.collector`, `JunitXml.TestLogger`
- Global using: `NUnit.Framework`

## Analyzer Configuration

Three config files layer the analyzer severity rules:

| File | `global_level` | Purpose |
|------|---------------|---------|
| `.globalconfig` | 100 (default) | Baseline severities for all projects |
| `src/roslyn-components.globalconfig` | 200 | Suppresses rules that don't apply to `netstandard2.0` analyzer projects |
| `tests/.globalconfig` | 200 | Relaxes rules that are noisy in test projects (e.g. CA1707 underscores in test names) |

Higher `global_level` wins on conflict. The overlays only suppress; they don't
promote warnings to errors.

## Code Style

`.editorconfig` at the repo root defines the code style applied during build
(`EnforceCodeStyleInBuild=true`) and enforced by IDEs:

- **Indentation:** 4 spaces, no tabs
- **Line endings:** LF (`end_of_line = lf`)
- **Encoding:** UTF-8 with BOM for C#, UTF-8 without BOM for other files
- **Braces:** Allman style
- **Namespaces:** file-scoped (`csharp_style_namespace_declarations = file_scoped`)
- **Naming:** PascalCase public members and constants, `_camelCase` private
  fields, `I` prefix on interfaces, `T` prefix on type parameters

## Local Pack Scripts

The `tools/` scripts produce packages locally for testing without going through CI:

| Script | Output |
|--------|--------|
| `tools/build-monads.ps1` | `packages/Monads.*.nupkg` |
| `tools/build-businessrules.ps1` | `packages/BusinessRules.*.nupkg` + analyzer package |
| `tools/build-unions.ps1` | `packages/Unions.*.nupkg` |

Output lands in `packages/` at the repo root (not committed). Use these to
test package consumption locally before merging.

## Analyzer Packaging Model

Roslyn analyzers and source generators cannot be shipped inside a regular
library `.nupkg` — they need specific NuGet packaging conventions to load
correctly in consuming projects.

The `BusinessRulesAnalyzer`, `BusinessRulesGenerator`, and
`BusinessRulesFixProvider` projects are marked `IsRoslynComponent=true` and
packed separately via `tools/BusinessRules.Analyzers.Package/`. The
`BusinessRules` library package then references the analyzer package as a
`PackageReference` with `PrivateAssets="all"`, so consumers get the analyzers
automatically without a direct reference.
