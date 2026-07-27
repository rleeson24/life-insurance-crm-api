param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$SqlServer,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true)]
    [string]$ContainerAppName
)

$ErrorActionPreference = 'Stop'

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

az sql db query `
    --resource-group $ResourceGroup `
    --server $SqlServer `
    --name $Database `
    --query $sql

Write-Host "Done."
