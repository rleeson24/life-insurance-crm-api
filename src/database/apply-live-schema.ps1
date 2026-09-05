# Applies live/*.sql in the same order as Aspire LiveSchemaScripts (001–012).
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
# Aspire (copy connection string from the BrokerBook database resource, not the sql server):
#
#   .\apply-live-schema.ps1 -ConnectionString "<BrokerBook connection string from dashboard>"
#
# Verify schema without applying scripts:
#
#   .\apply-live-schema.ps1 -ConnectionString "..." -VerifyOnly
param(
    [string]$ResourceGroup = 'rg-bbcrm-dev',
    [string]$SqlServer = '',
    [string]$Database = 'BrokerBook',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [string]$ConnectionString = '',
    [string]$Server = '',
    [switch]$UseIntegratedSecurity,
    [switch]$IncludeSeed,
    [switch]$SkipPublicAccessToggle,
    [switch]$VerifyOnly
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
    '011_PlanNameLists.sql',
    '012_ClientFieldEncryption.sql'
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

function Resolve-LiveSchemaConnectionString([string]$RawConnectionString) {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $RawConnectionString
    $catalog = [string]$builder['Initial Catalog']
    if ([string]::IsNullOrWhiteSpace($catalog) -or $catalog -eq 'master') {
        Write-Warning "Connection string targeted '$catalog'. Live schema applies to '$Database' (the Aspire API database). Overriding Initial Catalog."
        $builder['Initial Catalog'] = $Database
    }

    if (-not $builder.ContainsKey('TrustServerCertificate') -or -not $builder['TrustServerCertificate']) {
        $builder['TrustServerCertificate'] = $true
    }

    return [string]$builder.ConnectionString
}

function Test-LiveSchema {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection
    )

    $Connection.Open()
    try {
        $command = $Connection.CreateCommand()
        $command.CommandText = @"
SELECT c.name, TYPE_NAME(c.system_type_id) AS SqlType, c.max_length
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.Clients')
  AND c.name IN (
      N'DateOfBirth',
      N'MedicareNumber',
      N'MedicareNumberBlindIndex',
      N'MedicarePartAEffectiveDate',
      N'MedicarePartBEffectiveDate')
ORDER BY c.name;
"@
        $reader = $command.ExecuteReader()
        $columns = @{}
        while ($reader.Read()) {
            $columns[[string]$reader['name']] = [string]$reader['SqlType']
        }
        $reader.Close()

        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $Connection.ConnectionString
        $catalog = [string]$builder['Initial Catalog']
        Write-Host "Schema check on database '$catalog':"

        $expected = [ordered]@{
            DateOfBirth = 'varbinary'
            MedicareNumber = 'varbinary'
            MedicareNumberBlindIndex = 'varbinary'
            MedicarePartAEffectiveDate = 'varbinary'
            MedicarePartBEffectiveDate = 'varbinary'
        }

        $missing = @()
        $wrongType = @()
        foreach ($entry in $expected.GetEnumerator()) {
            if (-not $columns.ContainsKey($entry.Key)) {
                $missing += $entry.Key
                Write-Host "  MISSING  $($entry.Key)"
                continue
            }

            $actual = $columns[$entry.Key]
            if ($actual -ne $entry.Value) {
                $wrongType += "$($entry.Key) ($actual)"
                Write-Host "  WRONG    $($entry.Key) is $actual (expected $($entry.Value))"
            }
            else {
                Write-Host "  OK       $($entry.Key) ($actual)"
            }
        }

        if ($missing.Count -gt 0 -or $wrongType.Count -gt 0) {
            throw "Live schema is behind on '$catalog'. Missing: $($missing -join ', '). Wrong type: $($wrongType -join ', '). Re-run apply-live-schema.ps1 against the BrokerBook database connection string from the Aspire dashboard."
        }

        Write-Host "Live schema verification passed."
    }
    finally {
        $Connection.Close()
    }
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

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $Connection.ConnectionString
    $resolvedCatalog = [string]$builder['Initial Catalog']
    Write-Host "Done. Applied $($scriptFiles.Count) script(s) to '$resolvedCatalog'."
    Test-LiveSchema -Connection $Connection
}

if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    $resolvedConnectionString = Resolve-LiveSchemaConnectionString $ConnectionString
    $resolvedCatalog = [string](New-Object System.Data.SqlClient.SqlConnectionStringBuilder $resolvedConnectionString)['Initial Catalog']
    $connection = New-Object System.Data.SqlClient.SqlConnection $resolvedConnectionString
    if ($VerifyOnly) {
        Test-LiveSchema -Connection $connection
        return
    }

    Write-Host "Applying live schema to '$resolvedCatalog'..."
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
