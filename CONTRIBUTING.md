# Contributing to MaVe

Thank you for your interest in contributing to MaVe. This document
covers the workflow conventions, tooling setup, and guidelines for working in
this repository.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- [Node.js](https://nodejs.org/) (for Docusaurus documentation site)
- [GitHub CLI (`gh`)](https://cli.github.com/) for PR workflows
- [PowerShell 7+](https://github.com/PowerShell/PowerShell) for build scripts

## First-Time Setup

After cloning the repository, run the setup script:

```powershell
.\tools\setup.ps1
```

This configures:

- **Git hooks** — branch naming enforcement via `.githooks/pre-push`

## Branch Naming

All branches must use one of the following prefixes:

| Prefix      | Purpose                               |
|-------------|---------------------------------------|
| `docs/`     | Documentation, proposals, ADRs        |
| `feat/`     | New features                          |
| `fix/`      | Bug fixes                             |
| `refactor/` | Restructuring without behavior change |
| `chore/`    | CI, tooling, dependencies             |

The `pre-push` git hook enforces this convention. Pushes from branches without
a valid prefix will be rejected locally.

**Examples:**

```
docs/proposal-railway-api
feat/add-option-map
fix/result-null-handling
refactor/simplify-bind-chain
chore/update-ci-dotnet-version
```

## Commit Messages

Use the [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(optional-scope): <short description>
```

**Types** match the branch prefixes: `docs`, `feat`, `fix`, `refactor`, `chore`,
`test`.

**Examples:**

```
feat(monads): add Option.Map overload for async projections
fix(businessrules): handle null rule context in generator
docs: add proposal for railway-oriented API
chore(ci): add docs-only guard to ci-gate
test(monads): add Result.Bind edge case coverage
```

## Workflows

### Code Changes

Standard flow for any changes to source code, tests, or tooling:

```bash
git checkout main && git pull
git checkout -b feat/my-feature
# ... make changes ...
git add . && git commit -m "feat(scope): description"
git push origin HEAD
gh pr create --fill
```

The PR requires `ci-gate` to pass (build, test, docs validation) before it can
be squash-merged.

### Documentation Changes

Fast-track flow for docs-only changes (files in `docs/`, `README.md`,
`CONTRIBUTING.md`, `LICENSE`, `CHANGELOG.md`):

```bash
git checkout main && git pull
git checkout -b docs/my-change
# ... make changes ...
git add . && git commit -m "docs: description"
.\tools\docs-push.ps1
```

The `docs-push` script pushes, creates a PR, and enables auto-merge. The CI
pipeline validates that `docs/` branches only contain documentation files — if
non-docs files are present, `ci-gate` fails and auto-merge won't proceed.

## CI Pipeline

The CI pipeline (`.github/workflows/ci.yml`) runs on every PR and push to
`main`:

| Job               | Trigger                    | Purpose                                       |
|-------------------|----------------------------|-----------------------------------------------|
| `changes`         | Always                     | Detects which paths changed                   |
| `build-and-test`  | Code paths changed         | Builds solution, runs tests, uploads coverage |
| `docs-validate`   | Docs or code paths changed | Validates Docusaurus site builds              |
| `docs-only-guard` | PRs from `docs/` branches  | Rejects non-docs files on docs branches       |
| `ci-gate`         | Always                     | Aggregates results — required status check    |

The `ci-gate` job is the single required status check configured in the GitHub
branch ruleset. It aggregates results from all conditional jobs and fails if
any upstream job fails.

## Code Conventions

See [`.opencode/instructions.md`](.opencode/instructions.md) for detailed
architecture, coding conventions, and project principles.

Key points:

- Follow C# naming conventions (PascalCase public, `_camelCase` private fields)
- Use nullable reference types and XML documentation on public APIs
- Prefer `Result<T>` / `Option<T>` over exceptions in Monads
- Source generators over runtime reflection
- Each NuGet package must be independently consumable

For a deep-dive reference on the CI pipeline, versioning model, release
process, and build conventions, see the
[Ways of Working](https://mauroverberkt.github.io/MaVe/architecture/ways-of-working/developer-setup)
section in the documentation site.
