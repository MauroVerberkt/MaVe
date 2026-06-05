#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $repoRoot "packages"

if (-not (Test-Path -LiteralPath $outputDir))
{
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

dotnet pack "$repoRoot\src\Monads\Monads.csproj" --configuration Release --output $outputDir
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
