---
sidebar_position: 4
title: Package Structure
description: NuGet package composition, analyzer packing strategy, and build configuration
tags: [HelperMonads, BusinessRules]
keywords:
  - nuget
  - package
  - analyzer
  - build
  - msbuild
  - packaging
---

# Package Structure

## Overview

DotnetHelpers ships as three primary NuGet packages, each with specific internal layouts to support runtime code, analyzers, and build integration.

## HelperMonads Package

Simple library package - no analyzers or generators:

```
HelperMonads.nupkg
└── lib/
    └── net8.0/
        └── HelperMonads.dll
```

## BusinessRulesManagement Package

Complex package combining runtime library, source generator, analyzers, fix provider, schema, and build props:

```mermaid
graph TD
    subgraph "BusinessRulesManagement.nupkg"
        subgraph "lib/"
            L[net8.0/BusinessRules.dll]
        end
        subgraph "analyzers/dotnet/cs/"
            A1[BusinessRulesGenerator.dll]
            A2[BusinessRulesAnalyzer.dll]
            A3[BusinessRulesFixProvider.dll]
            A4[System.Text.Json.dll<br/><i>private dependency</i>]
        end
        subgraph "schemas/"
            S[businessrules-schema.json]
        end
        subgraph "build/"
            B[BusinessRulesManagement.props]
        end
    end

    style L fill:#7c3aed,color:#fff
    style A1 fill:#c4b5fd,color:#333
    style A2 fill:#c4b5fd,color:#333
    style A3 fill:#c4b5fd,color:#333
```

### Layout Explanation

| Path | Content | Purpose |
|------|---------|---------|
| `lib/net8.0/` | BusinessRules.dll | Runtime library consumed by user code |
| `analyzers/dotnet/cs/` | Generator + Analyzer + FixProvider DLLs | Loaded by the compiler during build |
| `analyzers/dotnet/cs/` | System.Text.Json.dll | Private dependency for the generator (netstandard2.0 needs it) |
| `schemas/` | businessrules-schema.json | JSON schema for IntelliSense in JSON editors |
| `build/` | BusinessRulesManagement.props | MSBuild props imported by consuming projects |

## How Analyzers Are Packed

The `BusinessRules.csproj` references the tooling projects with special attributes:

```xml
<ProjectReference Include="..\BusinessRulesAnalyzer\BusinessRulesAnalyzer.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

| Attribute | Effect |
|-----------|--------|
| `OutputItemType="Analyzer"` | Places the DLL in `analyzers/dotnet/cs/` in the package |
| `ReferenceOutputAssembly="false"` | Does NOT add a compile-time reference to the runtime project |

This means:
- The analyzer DLLs are included in the package but NOT referenced by BusinessRules.dll at compile time
- Consuming projects get the analyzers loaded by the compiler automatically
- The runtime library and tooling are completely decoupled at build time

## System.Text.Json Bundling

The generator needs `System.Text.Json` to parse the JSON files, but targets `netstandard2.0` which doesn't include it. Solution:

```xml
<!-- In BusinessRulesGenerator.csproj -->
<PackageReference Include="System.Text.Json"
                  GeneratePathProperty="true"
                  PrivateAssets="all" />

<Target Name="GetDependencyTargetPaths">
  <ItemGroup>
    <TargetPathWithTargetPlatformMoniker
      Include="$(PkgSystem_Text_Json)\lib\netstandard2.0\System.Text.Json.dll"
      IncludeRuntimeDependency="false" />
  </ItemGroup>
</Target>
```

`GeneratePathProperty="true"` creates a `$(PkgSystem_Text_Json)` MSBuild property pointing to the NuGet cache path. The custom target ensures the DLL is copied alongside the generator so the analyzer host can load it.

## Target Framework Strategy

```mermaid
graph LR
    subgraph "net8.0"
        RT[BusinessRules<br/>HelperMonads<br/>ResultExtensions<br/>Wcf]
    end

    subgraph "netstandard2.0"
        TL[Generator<br/>Analyzer<br/>FixProvider]
    end

    RT -.->|"consumed at runtime"| App[Consumer App]
    TL -.->|"loaded by compiler"| Compiler[Roslyn Compiler]
```

| Target | Used By | Reason |
|--------|---------|--------|
| `net8.0` | Runtime libraries | Access to latest C# features, nullable annotations |
| `netstandard2.0` | Tooling (analyzers, generators) | Required for compatibility with all Roslyn hosts (VS, Rider, CLI on any .NET version) |

## Build Configuration

### Schema Distribution

The JSON schema enables IntelliSense when editing `*.BusinessRules.json` files:

```xml
<None Include="schemas\businessrules-schema.json" Pack="true" PackagePath="schemas\" />
```

Consumers reference it in their JSON:
```json
{
  "$schema": "../schemas/businessrules-schema.json",
  "businessRules": [...]
}
```

### MSBuild Props

The `build/BusinessRulesManagement.props` file is automatically imported by consuming projects (NuGet convention). It can set defaults like:
- Adding the schema to known JSON schemas
- Configuring analyzer severity defaults
- Setting up AdditionalFiles patterns

## Extension Packages

Both extension packages are simple library packages:

```
BusinessRules.ResultExtensions.nupkg       BusinessRules.Wcf.nupkg
└── lib/                                   └── lib/
    └── net8.0/                                └── net8.0/
        └── BusinessRules.ResultExtensions.dll     └── BusinessRules.Wcf.dll
```

They declare package dependencies on their prerequisites:
- `ResultExtensions` depends on: `HelperMonads`, `BusinessRulesManagement`
- `Wcf` depends on: `BusinessRulesManagement`, `System.ServiceModel.Primitives`

## HelperUnions Package

Single package combining the attribute assembly, source generator, analyzer, and code fix provider. No private dependencies are bundled — unlike `BusinessRulesManagement`, the generator does not need `System.Text.Json`.

```mermaid
graph TD
    subgraph "HelperUnions.nupkg"
        subgraph "lib/"
            L[net8.0/HelperUnions.dll]
        end
        subgraph "analyzers/dotnet/cs/"
            A1[HelperUnionsGenerator.dll]
            A2[HelperUnionsAnalyzer.dll]
            A3[HelperUnionsFixProvider.dll]
        end
    end

    style L fill:#7c3aed,color:#fff
    style A1 fill:#c4b5fd,color:#333
    style A2 fill:#c4b5fd,color:#333
    style A3 fill:#c4b5fd,color:#333
```

### Layout Explanation

| Path | Content | Purpose |
|------|---------|---------|
| `lib/net8.0/` | HelperUnions.dll | `[Union]` attribute consumed by user code |
| `analyzers/dotnet/cs/` | Generator DLL | Emits union base, inspection API, and builder chains |
| `analyzers/dotnet/cs/` | Analyzer DLL | DNHU0001 (non-exhaustive switch) + DNHU0003 (invalid declaration) |
| `analyzers/dotnet/cs/` | FixProvider DLL | "Add missing union variant arms" code fix for DNHU0001 |

### Packing Strategy

`HelperUnions.csproj` uses a different packing strategy than `BusinessRules.csproj`. The `ProjectReference` entries only carry `ReferenceOutputAssembly="false"` (build ordering); the DLLs are packed via explicit `None Include` items:

```xml
<ItemGroup>
  <ProjectReference Include="..\HelperUnionsAnalyzer\HelperUnionsAnalyzer.csproj"
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\HelperUnionsFixProvider\HelperUnionsFixProvider.csproj"
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\HelperUnionsGenerator\HelperUnionsGenerator.csproj"
                    ReferenceOutputAssembly="false" />
</ItemGroup>

<ItemGroup>
  <None Include="..\HelperUnionsAnalyzer\bin\$(Configuration)\netstandard2.0\HelperUnionsAnalyzer.dll"
        Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  <None Include="..\HelperUnionsFixProvider\bin\$(Configuration)\netstandard2.0\HelperUnionsFixProvider.dll"
        Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  <None Include="..\HelperUnionsGenerator\bin\$(Configuration)\netstandard2.0\HelperUnionsGenerator.dll"
        Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```

| Attribute | Effect |
|-----------|--------|
| `ReferenceOutputAssembly="false"` | Ensures tooling projects build before packing, without adding a compile reference to `HelperUnions.dll` |
| `Pack="true" PackagePath="analyzers/dotnet/cs"` | Copies the built DLL directly into the correct analyzer slot in the package |
| `Visible="false"` | Hides the item from Solution Explorer |
