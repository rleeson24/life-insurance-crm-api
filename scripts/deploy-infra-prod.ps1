# Backward-compatible wrapper. Prefer: .\scripts\deploy-infra.ps1 -Environment prod
& "$PSScriptRoot\deploy-infra.ps1" -Environment prod @args
