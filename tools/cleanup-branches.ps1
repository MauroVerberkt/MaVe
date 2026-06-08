#Requires -Version 7.0
<#
.SYNOPSIS
    Remove stale local branches that have no remote and no unique work.

.DESCRIPTION
    A branch is considered safe to delete when BOTH of the following are true:
      1. It does not exist on the remote (origin).
      2. It is not ahead of main (i.e., all its commits are already in main).

    The script always runs 'git fetch --prune' first to ensure the local view
    of remote branches is current before making any decisions.

    Dry-run mode is on by default. Pass -Force to actually delete branches.

.PARAMETER Force
    Perform the deletions. Without this flag the script only reports what
    would be deleted.

.EXAMPLE
    .\tools\cleanup-branches.ps1
    # Dry run: lists branches that would be removed.

.EXAMPLE
    .\tools\cleanup-branches.ps1 -Force
    # Deletes the stale branches.
#>

param(
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

# ── Helpers ──────────────────────────────────────────────────────────────────

function Write-Header([string] $text)
{
    Write-Host ""
    Write-Host $text -ForegroundColor Cyan
    Write-Host ("-" * $text.Length) -ForegroundColor DarkGray
}

function Write-Keep([string] $branch, [string] $reason)
{
    Write-Host "  KEEP  $branch" -ForegroundColor DarkGray -NoNewline
    Write-Host "  ($reason)" -ForegroundColor DarkGray
}

function Write-Delete([string] $branch)
{
    Write-Host "  DEL   $branch" -ForegroundColor Yellow
}

function Write-Deleted([string] $branch)
{
    Write-Host "  DEL   $branch" -ForegroundColor Green
}

# ── Pre-flight ────────────────────────────────────────────────────────────────

$currentBranch = git symbolic-ref --short HEAD 2>$null
if (-not $currentBranch)
{
    Write-Error "Detached HEAD — checkout a branch before running this script."
    exit 1
}

if ($currentBranch -ne 'main')
{
    Write-Host "Switching to main..." -ForegroundColor Yellow
    git checkout main
}

Write-Host "Fetching and pruning remote refs..." -ForegroundColor Yellow
git fetch --prune
if ($LASTEXITCODE -ne 0)
{
    Write-Error "git fetch --prune failed."
    exit 1
}

# ── Collect branches ──────────────────────────────────────────────────────────

# All local branches except main
$localBranches = git branch --format '%(refname:short)' |
    Where-Object { $_ -ne 'main' }

# Branches that exist on origin right now (after prune)
$remoteBranches = git branch -r --format '%(refname:short)' |
    ForEach-Object { $_ -replace '^origin/', '' }

# ── Evaluate ──────────────────────────────────────────────────────────────────

$toDelete = [System.Collections.Generic.List[string]]::new()

Write-Header "Branch evaluation"

foreach ($branch in $localBranches)
{
    # Condition 1: not on remote
    $isOnRemote = $remoteBranches -contains $branch

    if ($isOnRemote)
    {
        Write-Keep $branch "exists on remote"
        continue
    }

    # Condition 2: not ahead of main (safe to remove)
    $aheadCount = git rev-list --count "main..$branch" 2>$null
    $isAheadOfMain = ($aheadCount -gt 0)

    if ($isAheadOfMain)
    {
        Write-Keep $branch "$aheadCount commit(s) not in main"
        continue
    }

    Write-Delete $branch
    $toDelete.Add($branch)
}

# ── Act ───────────────────────────────────────────────────────────────────────

if ($toDelete.Count -eq 0)
{
    Write-Host ""
    Write-Host "Nothing to clean up." -ForegroundColor Green
    exit 0
}

Write-Host ""

if (-not $Force)
{
    Write-Host "$($toDelete.Count) branch(es) would be deleted. Run with -Force to delete." -ForegroundColor Yellow
    exit 0
}

Write-Header "Deleting branches"

foreach ($branch in $toDelete)
{
    git branch -d $branch
    if ($LASTEXITCODE -ne 0)
    {
        # -d refuses branches not fully merged; use -D as a fallback since we
        # already verified no commits ahead of main.
        git branch -D $branch
    }
    Write-Deleted $branch
}

Write-Host ""
Write-Host "$($toDelete.Count) branch(es) deleted." -ForegroundColor Green
