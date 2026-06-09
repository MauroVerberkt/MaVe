#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $repoRoot "packages"

if (-not (Test-Path -LiteralPath $outputDir))
{
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

dotnet pack "$repoRoot\src\BusinessRules\BusinessRules.csproj" --configuration Release --output $outputDir
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
dotnet pack "$repoRoot\src\BusinessRules.ResultExtensions\BusinessRules.ResultExtensions.csproj" --configuration Release --output $outputDir
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
dotnet pack "$repoRoot\src\BusinessRules.Wcf\BusinessRules.Wcf.csproj" --configuration Release --output $outputDir
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
