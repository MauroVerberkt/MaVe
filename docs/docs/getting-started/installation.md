---
sidebar_position: 1
title: Installation
description: How to install and reference DotnetHelpers packages in your .NET project
keywords:
  - installation
  - nuget
  - dotnet
  - setup
---

# Installation

Add DotnetHelpers to your .NET project via NuGet or project references.

## NuGet Packages

```bash
dotnet add package HelperMonads
dotnet add package BusinessRulesManagement
dotnet add package HelperUnions
```

## Requirements

- .NET 8.0 or higher
- C# 12 or higher

## Project References (from source)

If you're working directly with the source code:

```xml
<ItemGroup>
  <ProjectReference Include="..\HelperMonads\HelperMonads.csproj" />
  <ProjectReference Include="..\BusinessRules\BusinessRules.csproj" />
  <ProjectReference Include="..\HelperUnions\HelperUnions.csproj" />
</ItemGroup>
```

### Optional packages

| Package | Purpose |
|---------|---------|
| `BusinessRules.ResultExtensions` | Bridge between BusinessRules and Result monad |
| `BusinessRules.Wcf` | WCF fault exception support (legacy systems) |

```xml
<ItemGroup>
  <ProjectReference Include="..\BusinessRules.ResultExtensions\BusinessRules.ResultExtensions.csproj" />
  <ProjectReference Include="..\BusinessRules.Wcf\BusinessRules.Wcf.csproj" />
</ItemGroup>
```

## Verify Installation

After adding references, verify everything compiles:

```bash
dotnet build
```

:::tip

The BusinessRules package includes Roslyn analyzers and a source generator. You'll get compile-time validation of business rule keys automatically.

:::

:::tip

The HelperUnions package includes a source generator, Roslyn analyzers (DNHU0001, DNHU0003), and a code fix provider. You'll get exhaustive matching and compile-time union validation automatically.

:::
