# Park or resume compute in a BrokerBook resource group.
# Stop turns off Container Apps so dev stops billing for replicas. Serverless
# SQL has no manual pause; with the API stopped it auto-pauses after its
# configured idle delay (60 minutes here). ACR, private endpoints, Key Vault,
# and the Static Web App stay in place.
# A later infra deploy resets Container App scale and can wake the API again.
param(
    [ValidateSet('Stop', 'Start')]
    [string]$Action = 'Stop',

    [string]$ResourceGroup = 'rg-bbcrm-dev',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [switch]$AllowProd
)

$ErrorActionPreference = 'Stop'
$ContainerAppApiVersion = '2026-01-01'

if ($ResourceGroup -match 'prod' -and -not $AllowProd) {
    throw "Refusing to $Action '$ResourceGroup'. Pass -AllowProd to override."
}

function Invoke-AzCli {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed (exit $LASTEXITCODE)."
    }
    return $output
}

function Get-AzTsv {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $raw = Invoke-AzCli -Arguments $Arguments
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return @()
    }
    return @($raw -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Wait-AzQuery {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string[]]$Desired,
        [Parameter(Mandatory = $true)]
        [string]$Label,
        [int]$TimeoutSeconds = 900
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $current = "$(Invoke-AzCli -Arguments $Arguments)".Trim()
        Write-Host "  $Label : $current"
        if ($Desired -contains $current) {
            return
        }
        if ((Get-Date) -ge $deadline) {
            throw "Timed out waiting for $Label to reach $($Desired -join ' or '). Last value: $current"
        }
        Start-Sleep -Seconds 15
    } while ($true)
}

function Get-ArmUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProviderPath,
        [Parameter(Mandatory = $true)]
        [string]$ApiVersion
    )

    return "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/${ProviderPath}?api-version=$ApiVersion"
}

$currentAccount = Invoke-AzCli -Arguments @('account', 'show', '--query', '{name:name, id:id}', '-o', 'json') | ConvertFrom-Json
if ($currentAccount.id -ne $SubscriptionId) {
    Write-Host "Switching subscription from $($currentAccount.name) ($($currentAccount.id)) to $SubscriptionId"
    Invoke-AzCli -Arguments @('account', 'set', '--subscription', $SubscriptionId) | Out-Null
    $currentAccount = Invoke-AzCli -Arguments @('account', 'show', '--query', '{name:name, id:id}', '-o', 'json') | ConvertFrom-Json
}

Write-Host "Subscription:   $($currentAccount.name) ($($currentAccount.id))"
Write-Host "Resource group: $ResourceGroup"
Write-Host "Action:         $Action"

$exists = Invoke-AzCli -Arguments @('group', 'exists', '--name', $ResourceGroup)
if ("$exists".Trim() -ne 'true') {
    throw "Resource group '$ResourceGroup' was not found."
}

$appNames = Get-AzTsv -Arguments @(
    'containerapp', 'list',
    '--resource-group', $ResourceGroup,
    '--query', '[].name',
    '-o', 'tsv'
)

$servers = Get-AzTsv -Arguments @(
    'sql', 'server', 'list',
    '--resource-group', $ResourceGroup,
    '--query', '[].name',
    '-o', 'tsv'
)

$databases = @()
foreach ($server in $servers) {
    $listed = Invoke-AzCli -Arguments @(
        'sql', 'db', 'list',
        '--resource-group', $ResourceGroup,
        '--server', $server,
        '--query', "[?name!='master'].{name:name, skuName:sku.name, status:status}",
        '-o', 'json'
    ) | ConvertFrom-Json
    foreach ($database in @($listed)) {
        $databases += [pscustomobject]@{
            Server  = $server
            Name    = $database.name
            SkuName = $database.skuName
            Status  = $database.status
        }
    }
}

function Get-ContainerAppRevisions {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $listed = Invoke-AzCli -Arguments @(
        'containerapp', 'revision', 'list',
        '--name', $Name,
        '--resource-group', $ResourceGroup,
        '--all',
        '--query', '[].{name:name, active:properties.active, created:properties.createdTime}',
        '-o', 'json'
    ) | ConvertFrom-Json
    return @($listed)
}

function Stop-ContainerApps {
    foreach ($name in $appNames) {
        $status = "$(Invoke-AzCli -Arguments @(
            'containerapp', 'show',
            '--name', $name,
            '--resource-group', $ResourceGroup,
            '--query', 'properties.runningStatus',
            '-o', 'tsv'
        ))".Trim()

        if ($status -ne 'Stopped') {
            Write-Host "Stopping container app $name ($status)..."
            $url = Get-ArmUrl -ProviderPath "Microsoft.App/containerApps/$name/stop" -ApiVersion $ContainerAppApiVersion
            Invoke-AzCli -Arguments @('rest', '--method', 'post', '--uri', $url, '-o', 'none') | Out-Null
            Wait-AzQuery -Label $name -Desired @('Stopped') -TimeoutSeconds 300 -Arguments @(
                'containerapp', 'show',
                '--name', $name,
                '--resource-group', $ResourceGroup,
                '--query', 'properties.runningStatus',
                '-o', 'tsv'
            )
        }
        else {
            Write-Host "Container app $name is already stopped."
        }

        # Stop leaves the revision active, so the next HTTP request can scale it back up.
        foreach ($revision in (Get-ContainerAppRevisions -Name $name | Where-Object { $_.active -eq $true })) {
            Write-Host "Deactivating $($revision.name) so traffic cannot start it again..."
            Invoke-AzCli -Arguments @(
                'containerapp', 'revision', 'deactivate',
                '--name', $name,
                '--resource-group', $ResourceGroup,
                '--revision', $revision.name
            ) | Out-Null
        }
    }
}

function Start-ContainerApps {
    foreach ($name in $appNames) {
        $revisions = Get-ContainerAppRevisions -Name $name
        $active = @($revisions | Where-Object { $_.active -eq $true })
        if ($active.Count -eq 0 -and $revisions.Count -gt 0) {
            $latest = $revisions | Sort-Object { $_.created } | Select-Object -Last 1
            Write-Host "Activating $($latest.name)..."
            Invoke-AzCli -Arguments @(
                'containerapp', 'revision', 'activate',
                '--name', $name,
                '--resource-group', $ResourceGroup,
                '--revision', $latest.name
            ) | Out-Null
        }

        $status = "$(Invoke-AzCli -Arguments @(
            'containerapp', 'show',
            '--name', $name,
            '--resource-group', $ResourceGroup,
            '--query', 'properties.runningStatus',
            '-o', 'tsv'
        ))".Trim()

        if ($status -eq 'Running') {
            Write-Host "Container app $name is already running."
            continue
        }

        Write-Host "Starting container app $name ($status)..."
        $url = Get-ArmUrl -ProviderPath "Microsoft.App/containerApps/$name/start" -ApiVersion $ContainerAppApiVersion
        Invoke-AzCli -Arguments @('rest', '--method', 'post', '--uri', $url, '-o', 'none') | Out-Null
        Wait-AzQuery -Label $name -Desired @('Running') -TimeoutSeconds 300 -Arguments @(
            'containerapp', 'show',
            '--name', $name,
            '--resource-group', $ResourceGroup,
            '--query', 'properties.runningStatus',
            '-o', 'tsv'
        )
    }
}

function Show-ServerlessDatabases {
    foreach ($database in $databases) {
        $label = "$($database.Server)/$($database.Name)"
        if ($database.SkuName -notlike '*_S_*') {
            Write-Host "Skipping $label ($($database.SkuName)). It is not serverless, so it cannot auto-pause."
            continue
        }

        $delay = "$(Invoke-AzCli -Arguments @(
            'sql', 'db', 'show',
            '--resource-group', $ResourceGroup,
            '--server', $database.Server,
            '--name', $database.Name,
            '--query', 'autoPauseDelay',
            '-o', 'tsv'
        ))".Trim()

        # -1 disables auto-pause. 60 minutes is the shortest delay General Purpose serverless allows.
        if ([string]::IsNullOrWhiteSpace($delay) -or $delay -eq '-1') {
            Write-Host "Enabling 60-minute auto-pause on $label..."
            Invoke-AzCli -Arguments @(
                'sql', 'db', 'update',
                '--resource-group', $ResourceGroup,
                '--server', $database.Server,
                '--name', $database.Name,
                '--auto-pause-delay', '60'
            ) | Out-Null
            $delay = '60'
        }

        if ($Action -eq 'Stop') {
            Write-Host "Database $label ($($database.SkuName)) auto-pauses after $delay minutes with no connections."
            Write-Host "Azure SQL Database serverless has no manual pause. Stopping the API is what lets that timer run out."
        }
        else {
            Write-Host "Database $label resumes on the first connection if it auto-paused. The first API request can take about a minute."
        }
    }
}

if ($Action -eq 'Stop') {
    Stop-ContainerApps
    Show-ServerlessDatabases
}
else {
    Start-ContainerApps
    Show-ServerlessDatabases
}

Write-Host ""
Write-Host "Result:"
foreach ($name in $appNames) {
    $status = "$(Invoke-AzCli -Arguments @(
        'containerapp', 'show',
        '--name', $name,
        '--resource-group', $ResourceGroup,
        '--query', 'properties.runningStatus',
        '-o', 'tsv'
    ))".Trim()
    Write-Host "  container app $name : $status"
}
foreach ($database in $databases) {
    $summary = Invoke-AzCli -Arguments @(
        'sql', 'db', 'show',
        '--resource-group', $ResourceGroup,
        '--server', $database.Server,
        '--name', $database.Name,
        '--query', '{status:status, autoPauseDelay:autoPauseDelay}',
        '-o', 'json'
    ) | ConvertFrom-Json
    Write-Host "  database $($database.Server)/$($database.Name) : $($summary.status), auto-pause $($summary.autoPauseDelay) min"
}

if ($Action -eq 'Stop') {
    Write-Host ""
    Write-Host "These resources stay allocated. They have no pause operation:"
    Write-Host "  - Container Registry (Basic SKU, about `$5/month)"
    Write-Host "  - Private endpoints for SQL and Key Vault (about `$7/month each)"
    Write-Host "  - Serverless SQL storage after the database auto-pauses"
    Write-Host "  - Log Analytics, only if something is still ingesting"
    Write-Host "Bring compute back with: .\scripts\park-environment.ps1 -Action Start"
}
