# Read-only check of a restored database on the production SQL server.
# Confirms row-level security hides clients with no tenant context, that one
# tenant can read its own client, and that a second tenant cannot read that client.
# Does not change the Entra admin or grant permissions. See grant-api-sql-access.ps1
# if the API identity is missing from the restored database.
# Use: .\verify-restored-database.ps1 -Database BrokerBook_XXXXXXXXXXXXXXXXX
param(
    [Parameter(Mandatory = $true)]
    [string]$Database,
    [string]$ResourceGroup = 'rg-bbcrm-prod',
    [string]$SqlServer = '',
    [string]$ContainerAppName = '',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [switch]$SkipPublicAccessToggle
)

$ErrorActionPreference = 'Stop'

function Open-EntraSqlConnection {
    param(
        [string]$Fqdn,
        [string]$DatabaseName,
        [string]$AccessToken
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = "Server=tcp:${Fqdn},1433;Initial Catalog=$DatabaseName;Encrypt=True;TrustServerCertificate=False;"
    $connection.AccessToken = $AccessToken
    $connection.Open()
    return $connection
}

function Invoke-SqlScalar {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [hashtable]$Parameters = @{}
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 60
    foreach ($name in $Parameters.Keys) {
        $value = $Parameters[$name]
        if ($value -is [guid]) {
            $parameter = $command.Parameters.Add("@$name", [System.Data.SqlDbType]::UniqueIdentifier)
        }
        else {
            $parameter = $command.Parameters.Add("@$name", [System.Data.SqlDbType]::NVarChar, 128)
        }

        $parameter.Value = $value
    }

    return $command.ExecuteScalar()
}

function Set-TenantContext {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [guid]$TenantId
    )

    [void](Invoke-SqlScalar -Connection $Connection -Sql "EXEC sys.sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;" -Parameters @{ tenantId = $TenantId })
}

function Get-SqlRows {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 60
    $reader = $command.ExecuteReader()
    try {
        $rows = @()
        while ($reader.Read()) {
            $row = [ordered]@{}
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $row[$reader.GetName($i)] = $reader.GetValue($i)
            }

            $rows += [pscustomobject]$row
        }

        return $rows
    }
    finally {
        $reader.Close()
    }
}

az account set --subscription $SubscriptionId | Out-Null

if ([string]::IsNullOrWhiteSpace($SqlServer)) {
    $SqlServer = az sql server list --resource-group $ResourceGroup --query '[0].name' -o tsv
    if ([string]::IsNullOrWhiteSpace($SqlServer)) {
        throw "No SQL server found in $ResourceGroup."
    }
}

if ([string]::IsNullOrWhiteSpace($ContainerAppName)) {
    $ContainerAppName = az containerapp list --resource-group $ResourceGroup --query "[?contains(name, '-api')].name | [0]" -o tsv
    if ([string]::IsNullOrWhiteSpace($ContainerAppName)) {
        throw "No API Container App found in $ResourceGroup."
    }
}

$fqdn = az sql server show --name $SqlServer --resource-group $ResourceGroup --query fullyQualifiedDomainName -o tsv
if ([string]::IsNullOrWhiteSpace($fqdn)) {
    throw "Could not resolve SQL server $SqlServer in $ResourceGroup."
}

$openedPublic = $false
$ruleName = 'verify-restore-sql'
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

    Write-Host "Checking $SqlServer / $Database..."

    $unscoped = Open-EntraSqlConnection -Fqdn $fqdn -DatabaseName $Database -AccessToken $token
    try {
        $visibleWithoutTenant = [int](Invoke-SqlScalar -Connection $unscoped -Sql 'SELECT COUNT(*) FROM dbo.Clients;')
        $readerMember = Invoke-SqlScalar -Connection $unscoped -Sql "SELECT IS_ROLEMEMBER('db_datareader', @name);" -Parameters @{ name = $ContainerAppName }
        $writerMember = Invoke-SqlScalar -Connection $unscoped -Sql "SELECT IS_ROLEMEMBER('db_datawriter', @name);" -Parameters @{ name = $ContainerAppName }
        $tenants = @(Get-SqlRows -Connection $unscoped -Sql 'SELECT TenantId, Name FROM dbo.Tenants WHERE IsDeleted = 0 ORDER BY Name;')
    }
    finally {
        $unscoped.Close()
    }

    if ($visibleWithoutTenant -ne 0) {
        throw "FAIL: $visibleWithoutTenant client rows are visible with no tenant context."
    }

    if ($readerMember -ne 1 -or $writerMember -ne 1) {
        throw "FAIL: [$ContainerAppName] is missing db_datareader or db_datawriter. Run grant-api-sql-access.ps1 -ResourceGroup $ResourceGroup -Database $Database"
    }

    $tenantsWithClients = @()
    foreach ($tenant in $tenants) {
        if ($tenantsWithClients.Count -ge 2) {
            break
        }

        $tenantId = [guid]$tenant.TenantId
        $scoped = Open-EntraSqlConnection -Fqdn $fqdn -DatabaseName $Database -AccessToken $token
        try {
            Set-TenantContext -Connection $scoped -TenantId $tenantId
            $clientCount = [int](Invoke-SqlScalar -Connection $scoped -Sql 'SELECT COUNT(*) FROM dbo.Clients WHERE IsDeleted = 0;')
        }
        finally {
            $scoped.Close()
        }

        if ($clientCount -gt 0) {
            $tenantsWithClients += [pscustomobject]@{
                TenantId = $tenantId
                Name = [string]$tenant.Name
                ClientCount = $clientCount
            }
        }
    }

    if ($tenantsWithClients.Count -lt 2) {
        throw 'FAIL: fewer than two tenants have an active client, so cross-tenant isolation cannot be shown.'
    }

    $tenantA = $tenantsWithClients[0]
    $tenantB = $tenantsWithClients[1]

    $connectionA = Open-EntraSqlConnection -Fqdn $fqdn -DatabaseName $Database -AccessToken $token
    try {
        Set-TenantContext -Connection $connectionA -TenantId $tenantA.TenantId
        $clientId = [guid](Invoke-SqlScalar -Connection $connectionA -Sql 'SELECT TOP (1) ClientId FROM dbo.Clients WHERE IsDeleted = 0;')
    }
    finally {
        $connectionA.Close()
    }

    $connectionB = Open-EntraSqlConnection -Fqdn $fqdn -DatabaseName $Database -AccessToken $token
    try {
        Set-TenantContext -Connection $connectionB -TenantId $tenantB.TenantId
        $visibleToOtherTenant = [int](Invoke-SqlScalar -Connection $connectionB -Sql 'SELECT COUNT(*) FROM dbo.Clients WHERE ClientId = @clientId;' -Parameters @{ clientId = $clientId })
    }
    finally {
        $connectionB.Close()
    }

    if ($visibleToOtherTenant -ne 0) {
        throw "FAIL: tenant $($tenantB.TenantId) can read client $clientId from tenant $($tenantA.TenantId)."
    }

    Write-Host 'PASS'
    Write-Host "Database: $Database"
    Write-Host "Clients visible with no tenant context: $visibleWithoutTenant"
    Write-Host "Tenant A: $($tenantA.Name) ($($tenantA.TenantId)); client $clientId; active clients $($tenantA.ClientCount)"
    Write-Host "Tenant B: $($tenantB.Name) ($($tenantB.TenantId)) cannot read that client; active clients $($tenantB.ClientCount)"
    Write-Host "API identity ${ContainerAppName}: db_datareader and db_datawriter"
}
finally {
    if ($openedPublic) {
        # CanNotDelete on the SQL server blocks firewall-rule delete. Narrow the rule
        # while public access is still on, then turn public access off. Azure rejects
        # firewall changes after public access is disabled.
        Write-Host 'Disabling public network access...'
        $previousErrorAction = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        az sql server firewall-rule update `
            --resource-group $ResourceGroup `
            --server $SqlServer `
            --name $ruleName `
            --start-ip-address 255.255.255.255 `
            --end-ip-address 255.255.255.255 `
            --only-show-errors | Out-Null
        $ruleExit = $LASTEXITCODE
        az sql server update --name $SqlServer --resource-group $ResourceGroup --enable-public-network false --only-show-errors | Out-Null
        $disableExit = $LASTEXITCODE
        $ErrorActionPreference = $previousErrorAction
        if ($disableExit -ne 0) {
            throw "Public network access is still enabled on $SqlServer. Turn it off before leaving this session."
        }

        if ($ruleExit -ne 0) {
            Write-Warning "Public access is off. Firewall rule $ruleName could not be narrowed and remains because the server lock blocks delete."
        }
        else {
            Write-Host "Public network access disabled. Firewall rule $ruleName remains under the server lock and no longer allows this machine."
        }
    }
}
