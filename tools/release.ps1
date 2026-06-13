#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Bump package version, open a PR, and publish the release tag.

.DESCRIPTION
    Full release flow in one script with a manual checkpoint:

      Phase 1 — Bump & PR:
        Creates a chore branch, bumps version.json, commits and pushes,
        opens a PR with auto-merge (squash) enabled.

      [Pause] — Waits for you to confirm the PR has merged.

      Phase 2 — Tag:
        Pulls main, queries NBGV for the real computed version (accounting
        for commit height), creates an annotated tag, and pushes it.

    Requires the nbgv CLI tool: dotnet tool restore

.EXAMPLE
    .\tools\release.ps1 -Package monads -Bump minor

.EXAMPLE
    .\tools\release.ps1 -Package businessrules -Version 0.2
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('monads', 'businessrules', 'unions', 'railyard')]
    [string]$Package,

    [ValidateSet('major', 'minor', 'patch')]
    [string]$Bump,

    [string]$Version
)

$ErrorActionPreference = 'Stop'

if (($Bump -and $Version) -or (-not $Bump -and -not $Version))
{
    throw 'Specify exactly one of -Bump or -Version.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot

$packageConfig = @{
    monads        = @{ VersionFile = 'src/Monads/version.json';        ProjectPath = 'src/Monads' }
    businessrules = @{ VersionFile = 'src/BusinessRules/version.json'; ProjectPath = 'src/BusinessRules' }
    unions        = @{ VersionFile = 'src/Unions/version.json';        ProjectPath = 'src/Unions' }
    railyard      = @{ VersionFile = 'src/Railyard/version.json';      ProjectPath = 'src/Railyard' }
}

$config          = $packageConfig[$Package]
$versionFilePath = Join-Path $repoRoot $config.VersionFile

# ─── Pre-flight ───────────────────────────────────────────────────────────────

$branch = git -C $repoRoot symbolic-ref --short HEAD 2> $null
if (-not $branch)
{
    throw 'Not on a branch (detached HEAD).'
}

if ($branch -ne 'main')
{
    throw "Must run from main. Currently on: $branch"
}

$status = git -C $repoRoot status --porcelain
if ($status)
{
    throw 'Working tree is not clean. Commit or stash changes first.'
}

if (-not (Test-Path -LiteralPath $versionFilePath))
{
    throw "Version file not found: $($config.VersionFile)"
}

# ─── Compute new base version ─────────────────────────────────────────────────

$versionJson     = Get-Content -LiteralPath $versionFilePath -Raw
$versionData     = $versionJson | ConvertFrom-Json
$currentVersionRaw = [string]$versionData.version

if (-not $currentVersionRaw)
{
    throw "Unable to read current version from: $($config.VersionFile)"
}

if ($Version)
{
    $newVersion = $Version.Trim()
}
else
{
    $parts = $currentVersionRaw.Split('.')
    if ($parts.Count -lt 2 -or $parts.Count -gt 3)
    {
        throw "Unsupported version format '$currentVersionRaw'. Expected 'major.minor' or 'major.minor.patch'."
    }

    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = if ($parts.Count -eq 3) { [int]$parts[2] } else { 0 }

    switch ($Bump)
    {
        'major' { $major++; $minor = 0; $patch = 0 }
        'minor' { $minor++; $patch = 0 }
        'patch' { $patch++ }
    }

    $newVersion = if ($parts.Count -eq 3) { "$major.$minor.$patch" } else { "$major.$minor" }
}

if ($newVersion -eq $currentVersionRaw)
{
    throw "New version matches current version: $newVersion"
}

if (-not ($newVersion -match '^\d+\.\d+(\.\d+)?$'))
{
    throw "Invalid version '$newVersion'. Expected 'major.minor' or 'major.minor.patch'."
}

# ─── Phase 1: Bump & PR ───────────────────────────────────────────────────────

$releaseBranch = "chore/$Package-v$newVersion-release"

Write-Host ""
Write-Host "Phase 1 — Bump & PR" -ForegroundColor Cyan
Write-Host "  Package : $Package"
Write-Host "  Version : $currentVersionRaw -> $newVersion"
Write-Host "  Branch  : $releaseBranch"
Write-Host ""

git -C $repoRoot checkout -b $releaseBranch
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$updatedVersionJson = [System.Text.RegularExpressions.Regex]::Replace(
    $versionJson,
    '"version"\s*:\s*"[^"]+"',
    ('"version": "{0}"' -f $newVersion),
    [System.Text.RegularExpressions.RegexOptions]::None,
    [TimeSpan]::FromSeconds(1)
)

if ($updatedVersionJson -eq $versionJson)
{
    throw "Failed to update version in $($config.VersionFile)"
}

Set-Content -LiteralPath $versionFilePath -Value $updatedVersionJson -NoNewline

git -C $repoRoot add $config.VersionFile
git -C $repoRoot commit -m "chore($Package): bump version to $newVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git -C $repoRoot push origin HEAD
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$prUrl = gh pr create --title "chore($Package): bump version to $newVersion" --body "" --base main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$prNumber = $prUrl -replace '.*/pull/(\d+).*', '$1'

gh pr merge --squash --auto --delete-branch
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "  PR #$prNumber created with auto-merge enabled." -ForegroundColor Green
Write-Host "  $prUrl"

# ─── Wait for merge ───────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Waiting for PR #$prNumber to merge..." -ForegroundColor Yellow
Write-Host "  Polling every 5 seconds. Press Ctrl+C to abort."
Write-Host ""

while ($true)
{
    $prState = (gh pr view $prNumber --json state --jq '.state').Trim()

    if ($prState -eq 'MERGED')
    {
        Write-Host "  PR #$prNumber merged." -ForegroundColor Green
        break
    }

    Write-Host "  State: $prState — checking again in 5s..."
    Start-Sleep -Seconds 5
}

# ─── Phase 2: Tag ─────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Phase 2 — Tag" -ForegroundColor Cyan

git -C $repoRoot checkout main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git -C $repoRoot pull
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$computedVersion = dotnet tool run nbgv get-version -p (Join-Path $repoRoot $config.ProjectPath) -v NuGetPackageVersion
if ($LASTEXITCODE -ne 0)
{
    throw "nbgv failed. Run 'dotnet tool restore' and try again."
}

$computedVersion = $computedVersion.Trim()
$tagName         = "$Package/v$computedVersion"

Write-Host "  Computed version : $computedVersion"
Write-Host "  Tag              : $tagName"

$existingTag = git -C $repoRoot tag --list $tagName
if ($existingTag)
{
    throw "Tag already exists: $tagName"
}

git -C $repoRoot tag -a $tagName -m "Release $Package $computedVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git -C $repoRoot push origin $tagName
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  Package : $Package" -ForegroundColor Cyan
Write-Host "  Version : $computedVersion" -ForegroundColor Green
Write-Host "  Tag     : $tagName pushed to origin." -ForegroundColor Green
