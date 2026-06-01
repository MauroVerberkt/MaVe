#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputDir = "$repoRoot\packages"

$packages = @(
    @{
        Name = "HelperUnions"
        Path = "$repoRoot\src\HelperUnions\HelperUnions.csproj"
        Description = "Source-generated discriminated unions for C#"
    }
)

Write-Host "Building HelperUnions Packages..." -ForegroundColor Cyan
Write-Host "Output directory: $outputDir" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$successCount = 0
$failureCount = 0

foreach ($package in $packages) {
    Write-Host "──────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "Building: $($package.Name)" -ForegroundColor Yellow
    Write-Host "Description: $($package.Description)" -ForegroundColor Gray

    Write-Host "Building and packing..." -ForegroundColor DarkYellow
    dotnet pack $package.Path --configuration Release --output $outputDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[SUCCESS] $($package.Name) created successfully" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "[FAILED] $($package.Name) failed to build" -ForegroundColor Red
        $failureCount++
    }
}

Write-Host ""
Write-Host "──────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host "Package Build Summary:" -ForegroundColor Cyan
Write-Host "  Succeeded: $successCount" -ForegroundColor Green
Write-Host "  Failed:    $failureCount" -ForegroundColor $(if ($failureCount -gt 0) { "Red" } else { "Gray" })

if ($successCount -gt 0) {
    Write-Host ""
    Write-Host "Created packages in: $outputDir" -ForegroundColor Cyan
    Get-ChildItem -Path $outputDir -Filter "HelperUnions*.nupkg" | Sort-Object LastWriteTime -Descending | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Green
    }
}

if ($failureCount -gt 0) {
    Write-Host ""
    Write-Host "Some packages failed to build!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "All HelperUnions packages built successfully!" -ForegroundColor Green
