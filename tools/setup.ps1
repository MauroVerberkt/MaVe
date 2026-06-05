#Requires -Version 7.0
<#
.SYNOPSIS
    One-time repository setup for MaVe development.

.DESCRIPTION
    Configures git hooks for branch naming enforcement.
    Run this once after cloning the repository.
#>

$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "MaVe — Developer Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Configure git hooks
Write-Host "Configuring git hooks..." -ForegroundColor Yellow
git config core.hooksPath .githooks
Write-Host "  [OK] core.hooksPath set to .githooks" -ForegroundColor Green

Write-Host ""
Write-Host "Setup complete." -ForegroundColor Green
Write-Host ""
Write-Host "Available tools:" -ForegroundColor Cyan
Write-Host "  .\tools\docs-push.ps1  — Fast-track a docs-only PR with auto-merge"
Write-Host ""
