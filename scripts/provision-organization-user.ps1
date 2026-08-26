# Inserts (or updates) an OrganizationUsers row so an Entra user can sign in.
# Azure SQL is private-endpoint only: this script briefly opens public access for
# your IP, runs T-SQL with an Entra token, then closes it again.
#
# Default: current az-login user as Admin on the development CRM tenant.
#
#   .\scripts\provision-organization-user.ps1
#
# Local SQL (no Azure networking changes):
#
#   .\scripts\provision-organization-user.ps1 -ConnectionString "Server=localhost,1433;Database=LifeInsuranceCRM;User Id=sa;Password=...;TrustServerCertificate=True"
param(
    [string]$ResourceGroup = 'rg-licrm-dev',
    [string]$SqlServer = '',
    [string]$Database = 'LifeInsuranceCRM',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [string]$UserId = '',
    [string]$EmailAddress = '',
    [string]$DisplayName = '',
    [string]$Role = 'Admin',
    [string]$TenantId = '22222222-2222-2222-2222-222222222222',
    [string]$TenantName = 'Development Tenant',
    [string]$ConnectionString = '',
    [switch]$SkipPublicAccessToggle
)

$ErrorActionPreference = 'Stop'

$allowedRoles = @('Admin', 'Agent', 'ReadOnly')
if ($allowedRoles -notcontains $Role) {
    throw "Role must be one of: $($allowedRoles -join ', ')."
}

$me = az ad signed-in-user show -o json | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($UserId)) {
    $UserId = [string]$me.id
}
if ([string]::IsNullOrWhiteSpace($EmailAddress)) {
    $EmailAddress = if ($me.mail) { [string]$me.mail } else { [string]$me.userPrincipalName }
}
if ([string]::IsNullOrWhiteSpace($DisplayName)) {
    $DisplayName = [string]$me.displayName
}

try {
    $userGuid = [guid]$UserId
    $tenantGuid = [guid]$TenantId
}
catch {
    throw 'UserId and TenantId must be GUIDs. UserId is the Entra object ID (oid), not sub/NameIdentifier.'
}

$systemUserId = [guid]'00000000-0000-0000-0000-000000000001'

function Invoke-ProvisionSql {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection
    )

    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 60
        $command.CommandText = @'
IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE TenantId = @TenantId)
BEGIN
    INSERT INTO dbo.Tenants (TenantId, Name, CreatedByUserId, UpdatedByUserId)
    VALUES (@TenantId, @TenantName, @SystemUserId, @SystemUserId);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.OrganizationUsers WHERE UserId = @UserId AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.OrganizationUsers (
        TenantId, UserId, EmailAddress, DisplayName, Role, IsActive,
        CreatedByUserId, UpdatedByUserId)
    VALUES (
        @TenantId, @UserId, @EmailAddress, @DisplayName, @Role, 1,
        @SystemUserId, @SystemUserId);
END
ELSE
BEGIN
    UPDATE dbo.OrganizationUsers
    SET TenantId = @TenantId,
        EmailAddress = @EmailAddress,
        DisplayName = @DisplayName,
        Role = @Role,
        IsActive = 1,
        IsDeleted = 0,
        DeletedAt = NULL,
        DeletedByUserId = NULL,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedByUserId = @SystemUserId
    WHERE UserId = @UserId AND IsDeleted = 0;
END;

SELECT OrganizationUserId, TenantId, UserId, EmailAddress, DisplayName, Role, IsActive
FROM dbo.OrganizationUsers
WHERE UserId = @UserId AND IsDeleted = 0;
'@
        $null = $command.Parameters.Add('@TenantId', [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters['@TenantId'].Value = $tenantGuid
        $null = $command.Parameters.Add('@TenantName', [System.Data.SqlDbType]::NVarChar, 200)
        $command.Parameters['@TenantName'].Value = $TenantName
        $null = $command.Parameters.Add('@SystemUserId', [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters['@SystemUserId'].Value = $systemUserId
        $null = $command.Parameters.Add('@UserId', [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters['@UserId'].Value = $userGuid
        $null = $command.Parameters.Add('@EmailAddress', [System.Data.SqlDbType]::NVarChar, 320)
        $command.Parameters['@EmailAddress'].Value = $EmailAddress
        $null = $command.Parameters.Add('@DisplayName', [System.Data.SqlDbType]::NVarChar, 200)
        $command.Parameters['@DisplayName'].Value = $DisplayName
        $null = $command.Parameters.Add('@Role', [System.Data.SqlDbType]::NVarChar, 50)
        $command.Parameters['@Role'].Value = $Role

        $reader = $command.ExecuteReader()
        if (-not $reader.Read()) {
            throw 'Provisioning finished but no OrganizationUsers row was returned.'
        }

        Write-Host "Provisioned $($reader['EmailAddress']) ($($reader['UserId'])) as $($reader['Role']) on CRM tenant $($reader['TenantId'])."
        $reader.Close()
    }
    finally {
        $connection.Close()
    }
}

if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "Provisioning $EmailAddress ($UserId) via connection string..."
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    Invoke-ProvisionSql -Connection $connection
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
$principalId = [string]$me.id
$upn = [string]$me.userPrincipalName

Write-Host "Ensuring Entra admin on $SqlServer is $upn ($principalId)..."
az sql server ad-admin create `
    --resource-group $ResourceGroup `
    --server-name $SqlServer `
    --display-name $upn `
    --object-id $principalId `
    --only-show-errors | Out-Null

$openedPublic = $false
$ruleName = 'bootstrap-provision-user'
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

    Write-Host "Provisioning $EmailAddress ($UserId) on $SqlServer / $Database..."
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = "Server=tcp:$fqdn,1433;Initial Catalog=$Database;Encrypt=True;TrustServerCertificate=False;"
    $connection.AccessToken = $token
    Invoke-ProvisionSql -Connection $connection
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
