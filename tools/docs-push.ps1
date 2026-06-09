#Requires -Version 7.0
<#
.SYNOPSIS
    Fast-track a docs-only PR with auto-merge.

.DESCRIPTION
    Pushes the current branch, creates a PR, and sets it to auto-merge via squash.
    Intended for docs/ branches that only touch documentation files.

    The CI pipeline validates that docs/ branches contain only documentation changes.
    If non-docs files are included, ci-gate will fail and auto-merge won't proceed.

.EXAMPLE
    git checkout -b docs/proposal-new-feature
    # ... make changes ...
    git add . && git commit -m "docs: add proposal for new feature"
    .\tools\docs-push.ps1
#>

$ErrorActionPreference = 'Stop'

# Verify we're on a docs/ branch
$branch = git symbolic-ref --short HEAD 2> $null
if (-not $branch)
{
    Write-Error "Not on a branch (detached HEAD)."
    exit 1
}

if (-not $branch.StartsWith("docs/"))
{
    Write-Error "This script is for docs/ branches only. Current branch: $branch"
    exit 1
}

# Verify there are no uncommitted changes
$status = git status --porcelain
if ($status)
{
    Write-Error "Working tree is not clean. Commit or stash changes first."
    exit 1
}

Write-Host "Pushing $branch..." -ForegroundColor Yellow
git push origin HEAD

Write-Host "Creating PR..." -ForegroundColor Yellow
gh pr create --fill

Write-Host "Enabling auto-merge..." -ForegroundColor Yellow
gh pr merge --squash --auto --delete-branch

Write-Host ""
Write-Host "Done. PR will auto-merge once ci-gate passes." -ForegroundColor Green
