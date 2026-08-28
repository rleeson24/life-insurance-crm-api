# Creates an Entra contained user for the API Container App identity and grants
# db_datareader / db_datawriter. Requires an Entra admin on the SQL server
# (sqlAzureAdAdministratorObjectId in Bicep, or set via this script).
param(
    [string]$ResourceGroup = 'rg-licrm-dev',
    [string]$SqlServer = '',
    [string]$Database = 'BrokerBook',
    [string]$ContainerAppName = 'licrm-dev-api',
    [string]$SubscriptionId = '605a6796-5cf0-4a61-80f0-ff2d484360ee',
    [switch]$SkipPublicAccessToggle
)

$ErrorActionPreference = 'Stop'

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
$ruleName = 'bootstrap-grant-sql'
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

    $sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$ContainerAppName')
BEGIN
    CREATE USER [$ContainerAppName] FROM EXTERNAL PROVIDER;
END;

IF IS_ROLEMEMBER('db_datareader', '$ContainerAppName') <> 1
    ALTER ROLE db_datareader ADD MEMBER [$ContainerAppName];

IF IS_ROLEMEMBER('db_datawriter', '$ContainerAppName') <> 1
    ALTER ROLE db_datawriter ADD MEMBER [$ContainerAppName];
"@

    Write-Host "Granting database access to managed identity [$ContainerAppName] on $SqlServer / $Database..."

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = "Server=tcp:$fqdn,1433;Initial Catalog=$Database;Encrypt=True;TrustServerCertificate=False;"
    $connection.AccessToken = $token
    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $command.CommandTimeout = 60
        [void]$command.ExecuteNonQuery()
    } finally {
        $connection.Close()
    }

    Write-Host 'Done. Restart the Container App if /health is still failing.'
} finally {
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
