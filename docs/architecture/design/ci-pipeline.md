---
sidebar_position: 5
title: CI Pipeline
description: GitHub Actions CI/CD architecture, coverage reporting, and branch protection
tags: [infra, testing, Monads, BusinessRules]
keywords:
  - ci
  - github-actions
  - codecov
  - coverage
  - pipeline
---

# CI Pipeline

## Overview

Every change to this repository goes through a CI pipeline that builds the solution, runs all tests with coverage collection, validates the documentation site, and reports results to Codecov. Direct pushes to `main` are not allowed — all changes require a pull request with passing checks.

## Workflow Architecture

The pipeline uses a **reusable workflow pattern** for extensibility. Future workflows (release, SonarCloud, benchmarks) can call the same building blocks without duplicating steps.

```
.github/workflows/
  ci.yml                  # Orchestrator: path filtering, job dispatch, gate
  _build-and-test.yml     # Reusable: restore -> build -> test -> coverage -> report
  _docs-validate.yml      # Reusable: npm ci -> Docusaurus build
```

The `_` prefix convention indicates workflows that are not triggered directly — they are only called by other workflows via `workflow_call`.

### ci.yml (Orchestrator)

Triggers on `pull_request` and `push` to `main`. Responsibilities:

1. **Path detection** — uses `dorny/paths-filter` to classify changes as `code`, `docs`, or both
2. **Job dispatch** — calls reusable workflows conditionally based on what changed
3. **Gate aggregation** — the `ci-gate` job always runs and reports a single pass/fail for branch protection

### _build-and-test.yml

Accepts inputs:
- `publish-summary` (boolean) — whether to generate `test-summary.json` (only on main)
- `CODECOV_TOKEN` (secret) — for coverage upload

Steps: checkout -> setup .NET -> NuGet cache -> restore -> build -> test with coverage -> upload to Codecov -> upload test results -> generate summary artifact.

### _docs-validate.yml

Steps: checkout -> setup Node.js -> npm ci -> Docusaurus build. Catches broken MDX, dead links, and frontmatter errors before merge.

:::note
Each reusable workflow includes `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true` as an env var since the caller's env does not propagate to called workflows.
:::

## Path Filtering

Not all changes need all jobs:

| What changed | Jobs that run |
|--------------|---------------|
| Code (`src/`, `tests/`, `*.sln`, etc.) | `build-and-test` + `docs-validate` |
| Docs only (`docs/`) | `docs-validate` only |
| Push to main (post-merge) | Both + artifact publishing |

Code PRs always validate docs too, since code changes can affect API documentation or generated content.

## Gate Job

The `ci-gate` job solves a GitHub Actions limitation: conditional jobs that are skipped never report a status, which blocks required checks. The gate:

- Always runs (`if: always()`)
- Depends on both `build-and-test` and `docs-validate`
- Passes if upstream jobs succeeded **or were skipped** (legitimate skip = docs-only PR)
- Fails if any upstream job failed

This is the **only required status check** in branch protection.

## Coverage Reporting

### Coverlet Configuration

All test projects include `coverlet.collector`. Coverage is collected in two formats simultaneously:
- **Cobertura** — uploaded to Codecov
- **OpenCover** — available for local ReportGenerator use if needed

### Test Results

Test projects include `JunitXml.TestLogger` (added via `tests/Directory.Build.props`). JUnit XML results are uploaded to Codecov Test Analytics for:
- Pass/fail visibility per PR
- Duration trends over time
- Flaky test detection

### Codecov Integration

Two calls to `codecov/codecov-action@v5` are used:
- First call: uploads coverage data (Cobertura XML)
- Second call: uploads JUnit test results with `report_type: test_results`

Dashboard: [app.codecov.io/github/MauroVerberkt/MaVe](https://app.codecov.io/github/MauroVerberkt/MaVe)

## Branch Protection

Enforced via GitHub repository ruleset (`.github/ruleset-protect-main.json`):

- **Pull requests required** — no direct pushes to main
- **Required status check** — `ci-gate` must pass
- **Branches must be up to date** — ensures PRs are tested against latest main
- **No force push** — history is immutable

## Running Locally

```bash
# Build
dotnet build --configuration Release

# Run tests with coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Validate docs
cd docs
npm ci
npm run build
```

## Extending the Pipeline

### Adding a new reusable workflow

1. Create `.github/workflows/_your-workflow.yml` with `on: workflow_call`
2. Call it from `ci.yml` or a new orchestrator (e.g., `release.yml`)
3. If it should gate merging, add it to `ci-gate`'s `needs` array

### Planned extensions

- **Release workflow** — calls `_build-and-test.yml`, then `dotnet pack` + NuGet push
- **SonarCloud** — separate workflow (SonarScanner wraps the build, can't reuse `_build-and-test.yml` directly)
- **Benchmarks** — BenchmarkDotNet runs with results published to GitHub Pages

### Test summary artifact

On pushes to main, the pipeline generates `test-summary.json` containing coverage %, test count, and timestamp. This is the interface contract for the docs site's future health dashboard component (see PROP-008 stretch goal).
