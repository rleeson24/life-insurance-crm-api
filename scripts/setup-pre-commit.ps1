# Installs pre-commit hooks using python -m (works when pre-commit is not on PATH).
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "Checking for pre-commit..."
python -m pre_commit --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Installing pre-commit..."
    python -m pip install pre-commit
}

Write-Host "Installing git hooks in $repoRoot"
python -m pre_commit install

Write-Host ""
Write-Host "Done. Hooks run automatically on git commit."
Write-Host "Optional: python -m pre_commit run --all-files"
