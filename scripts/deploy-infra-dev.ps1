# Backward-compatible wrapper. Prefer: .\scripts\deploy-infra.ps1 -Environment dev
& "$PSScriptRoot\deploy-infra.ps1" -Environment dev @args
