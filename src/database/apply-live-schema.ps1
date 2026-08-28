# Applies live/*.sql in the same order as Aspire LiveSchemaScripts (001–011).
# Scripts are idempotent; re-running is safe.
#
# Azure SQL (private endpoint): Entra token, brief public access for your IP, then close.
#
#   cd src/database
#   .\apply-live-schema.ps1
#
# Include the development seed user (skip on Azure unless you need it):
#
#   .\apply-live-schema.ps1 -IncludeSeed
#
# Local SQL Server (Windows auth):
#
#   .\apply-live-schema.ps1 -Server "localhost,1433" -UseIntegratedSecurity -IncludeSeed
#
# Explicit connection string:
#
#   .\apply-live-schema.ps1 -ConnectionString "Server=localhost,1433;Database=BrokerBook;..." -IncludeSeed
param(
    [string]$ResourceGroup = 'rg-bbcrm-dev',
    [string]$SqlServer = '',
    [string]$Database = 'BrokerBook',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [string]$ConnectionString = '',
    [string]$Server = '',
    [switch]$UseIntegratedSecurity,
    [switch]$IncludeSeed,
    [switch]$SkipPublicAccessToggle
)

$ErrorActionPreference = 'Stop'

$liveRoot = Join-Path $PSScriptRoot 'live'
$scriptFiles = @(
    '001_Tenants.sql',
    '002_Clients.sql',
    '003_ClientInteractions.sql',
    '004_MajorMedicalEnrollments.sql',
    '005_SecondaryEnrollments.sql',
    '006_DrugPlanEnrollments.sql',
    '007_OrganizationUsers.sql',
    '008_AuthSecurityEvents.sql',
    '009_RLS.sql',
    '010_OrganizationUserRoles.sql',
    '011_PlanNameLists.sql'
)

if ($IncludeSeed) {
    $scriptFiles += 'seed/001_DevelopmentTenant.sql'
}

foreach ($scriptFile in $scriptFiles) {
    $path = Join-Path $liveRoot $scriptFile
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Live schema script not found: $path"
    }
}

function Split-SqlBatches([string]$sql) {
    $batches = [System.Collections.Generic.List[string]]::new()
    $current = [System.Text.StringBuilder]::new()
    foreach ($line in ($sql -split '\r?\n')) {
        if ($line -match '^\s*GO\s*$') {
            $batch = $current.ToString().Trim()
            if ($batch.Length -gt 0) {
                $batches.Add($batch)
            }
            [void]$current.Clear()
            continue
        }
        [void]$current.AppendLine($line)
    }

    $tail = $current.ToString().Trim()
    if ($tail.Length -gt 0) {
        $batches.Add($tail)
    }

    return $batches
}

function Invoke-LiveSchema {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection
    )

    $Connection.Open()
    try {
        foreach ($scriptFile in $scriptFiles) {
            $path = Join-Path $liveRoot $scriptFile
            Write-Host "--- $scriptFile ---"
            $sql = [System.IO.File]::ReadAllText($path)
            foreach ($batch in (Split-SqlBatches $sql)) {
                $command = $Connection.CreateCommand()
                $command.CommandText = $batch
                $command.CommandTimeout = 120
                [void]$command.ExecuteNonQuery()
            }
        }
    }
    finally {
        $Connection.Close()
    }

    Write-Host "Done. Applied $($scriptFiles.Count) script(s) to $Database."
}

if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "Applying live schema via connection string to $Database..."
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    Invoke-LiveSchema -Connection $connection
    return
}

if ($UseIntegratedSecurity -or -not [string]::IsNullOrWhiteSpace($Server)) {
    if ([string]::IsNullOrWhiteSpace($Server)) {
        $Server = 'localhost,1433'
    }

    Write-Host "Applying live schema to $Server / $Database (integrated security)..."
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source'] = $Server
    $builder['Initial Catalog'] = $Database
    $builder['Integrated Security'] = $true
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = $true
    $connection = New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString
    Invoke-LiveSchema -Connection $connection
    return
}

az account set --subscription $SubscriptionId | Out-Null

if ([string]::IsNullOrWhiteSpace($SqlServer)) {
    $SqlServer = az sql server list --resource-group $ResourceGroup --query '[0].name' -o tsv
    if ([string]::IsNullOrWhiteSpace($SqlServer)) {
        throw "No SQL server found in $ResourceGroup."
    }
}

$fqdn = az sql server show --name $SqlServer --resource-group $ResourceGroup --query fullyQualifiedDomainName -o tsv
$principalId = az ad signed-in-user show --query id -o tsv
$upn = az ad signed-in-user show --query userPrincipalName -o tsv

Write-Host "Ensuring Entra admin on $SqlServer is $upn ($principalId)..."
az sql server ad-admin create `
    --resource-group $ResourceGroup `
    --server-name $SqlServer `
    --display-name $upn `
    --object-id $principalId `
    --only-show-errors | Out-Null

$openedPublic = $false
$ruleName = 'bootstrap-apply-schema'
try {
    $publicAccess = az sql server show --name $SqlServer --resource-group $ResourceGroup --query publicNetworkAccess -o tsv
    if (-not $SkipPublicAccessToggle -and $publicAccess -eq 'Disabled') {
        Write-Host 'Temporarily enabling public network access so this machine can run T-SQL...'
        az sql server update --name $SqlServer --resource-group $ResourceGroup --enable-public-network true --only-show-errors | Out-Null
        $openedPublic = $true
        $clientIp = (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).Trim()
        az sql server firewall-rule create `
            --resource-group $ResourceGroup `
            --server $SqlServer `
            --name $ruleName `
            --start-ip-address $clientIp `
            --end-ip-address $clientIp `
            --only-show-errors | Out-Null
        Write-Host "Firewall rule $ruleName for $clientIp"
        Start-Sleep -Seconds 15
    }

    $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw 'Could not get an Azure AD token for database.windows.net. Run az login.'
    }

    Write-Host "Applying live schema to $fqdn / $Database..."
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = "Server=tcp:$fqdn,1433;Initial Catalog=$Database;Encrypt=True;TrustServerCertificate=False;"
    $connection.AccessToken = $token
    Invoke-LiveSchema -Connection $connection
}
finally {
    if ($openedPublic) {
        Write-Host 'Removing bootstrap firewall rule and disabling public network access...'
        az sql server firewall-rule delete `
            --resource-group $ResourceGroup `
            --server $SqlServer `
            --name $ruleName `
            --only-show-errors 2>$null
        az sql server update --name $SqlServer --resource-group $ResourceGroup --enable-public-network false --only-show-errors | Out-Null
    }
}
