#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Bump package version and create release tag.

.DESCRIPTION
    Updates the package version.json, commits the version bump, and creates
    an annotated git tag that matches MaVe's NBGV release refspec.

.EXAMPLE
    .\tools\release.ps1 -Package monads -Bump minor

.EXAMPLE
    .\tools\release.ps1 -Package businessrules -Version 0.2

.EXAMPLE
    .\tools\release.ps1 -Package railyard -Bump patch -Push
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('monads', 'businessrules', 'unions', 'railyard')]
    [string]$Package,

    [ValidateSet('major', 'minor', 'patch')]
    [string]$Bump,

    [string]$Version,

    [switch]$Push
)

$ErrorActionPreference = 'Stop'

if (($Bump -and $Version) -or (-not $Bump -and -not $Version))
{
    throw 'Specify exactly one of -Bump or -Version.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFileByPackage = @{
    monads = 'src/Monads/version.json'
    businessrules = 'src/BusinessRules/version.json'
    unions = 'src/Unions/version.json'
    railyard = 'src/Railyard/version.json'
}

$versionFileRelative = $versionFileByPackage[$Package]
$versionFilePath = Join-Path $repoRoot $versionFileRelative

if (-not (Test-Path -LiteralPath $versionFilePath))
{
    throw "Version file not found: $versionFilePath"
}

$branch = git -C $repoRoot symbolic-ref --short HEAD 2> $null
if (-not $branch)
{
    throw 'Not on a branch (detached HEAD).'
}

$status = git -C $repoRoot status --porcelain
if ($status)
{
    throw 'Working tree is not clean. Commit or stash changes first.'
}

$versionJson = Get-Content -LiteralPath $versionFilePath -Raw
$versionData = $versionJson | ConvertFrom-Json
$currentVersionRaw = [string]$versionData.version

if (-not $currentVersionRaw)
{
    throw "Unable to read current version from: $versionFileRelative"
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
        'major'
        {
            $major++
            $minor = 0
            $patch = 0
        }
        'minor'
        {
            $minor++
            $patch = 0
        }
        'patch'
        {
            $patch++
        }
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

$tagName = "$Package/v$newVersion"

$existingTag = git -C $repoRoot tag --list $tagName
if ($existingTag)
{
    throw "Tag already exists: $tagName"
}

$updatedVersionJson = [System.Text.RegularExpressions.Regex]::Replace(
    $versionJson,
    '"version"\s*:\s*"[^"]+"',
    ('"version": "{0}"' -f $newVersion),
    [System.Text.RegularExpressions.RegexOptions]::None,
    [TimeSpan]::FromSeconds(1)
)

if ($updatedVersionJson -eq $versionJson)
{
    throw "Failed to update version in $versionFileRelative"
}

Set-Content -LiteralPath $versionFilePath -Value $updatedVersionJson -NoNewline

git -C $repoRoot add $versionFileRelative
git -C $repoRoot commit -m "chore($Package): bump version to $newVersion"
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

git -C $repoRoot tag -a $tagName -m "Release $Package $newVersion"
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

if ($Push)
{
    git -C $repoRoot push origin HEAD
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

    git -C $repoRoot push origin $tagName
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }
}

Write-Host "Package: $Package" -ForegroundColor Cyan
Write-Host "Version: $currentVersionRaw -> $newVersion" -ForegroundColor Green
Write-Host "Tag: $tagName" -ForegroundColor Green
if ($Push)
{
    Write-Host 'Pushed commit and tag to origin.' -ForegroundColor Green
}
else
{
    Write-Host 'Commit and tag created locally. Use -Push to publish.' -ForegroundColor Yellow
}
