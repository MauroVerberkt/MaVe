---
sidebar_position: 1
title: Developer Setup
---

# Developer Setup

Everything needed to go from a fresh clone to a working dev environment.

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ (9.0 recommended) | Build and test |
| [Node.js](https://nodejs.org/) | 20+ | Docusaurus docs site |
| [GitHub CLI (`gh`)](https://cli.github.com/) | Latest | PR workflows, `docs-push.ps1` |
| [PowerShell 7+](https://github.com/PowerShell/PowerShell) | 7.0+ | Build scripts |

## First-Time Setup

After cloning:

```powershell
.\tools\setup.ps1
```

This script:

1. Sets `core.hooksPath` to `.githooks` — enables branch-naming enforcement
   via the `pre-push` hook

That's it. No global tool installs, no Docker, no database.

## Git Hook: `pre-push`

The `.githooks/pre-push` script rejects pushes from branches without a valid
prefix. It runs locally before the push reaches GitHub.

**Allowed prefixes:** `docs/`, `feat/`, `fix/`, `refactor/`, `chore/`

If the hook fires unexpectedly:

- Verify your branch name: `git symbolic-ref --short HEAD`
- Rename if needed: `git branch -m <correct-prefix>/name`
- The hook does **not** run on `main` or detached HEAD

## IDE Recommendations

Any IDE works, but these extensions improve the experience:

**Visual Studio / Rider:**
- Roslynator extension (mirrors the `Roslynator.Analyzers` package in CI)
- EditorConfig support (built-in in both)

**VS Code:**
- C# Dev Kit
- EditorConfig for VS Code
- Mermaid Markdown Preview (for architecture docs)

## Verifying Your Setup

```powershell
# Build entire solution
dotnet build

# Run all tests (net8.0 + net9.0)
dotnet test

# Build docs site locally
cd docs
npm ci
npm run build
```

All three should pass with zero errors on a fresh clone.
