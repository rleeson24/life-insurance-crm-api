# Azure runtime auth (PR 3)

Production Container Apps authenticate to Azure SQL and Key Vault with the API's **system-assigned managed identity**. Local development continues to use Aspire/SQL login connection strings and skips Key Vault when `KeyVault:VaultUri` is unset.

## How it works

| Environment | SQL auth | Secrets |
|-------------|----------|---------|
| Local (Aspire) | `ConnectionStrings:LifeInsuranceCRM` from AppHost | User secrets / appsettings |
| Azure Container Apps | `Database:Server` + `Database:Name` → Active Directory Default | Key Vault via `KeyVault:VaultUri` |

Startup order:

1. Read `KeyVault:VaultUri` from environment (`KeyVault__VaultUri` in Container Apps).
2. If set, register the Azure Key Vault configuration provider (`DefaultAzureCredential`).
3. Resolve the effective SQL connection string (`DatabaseConnectionStringResolver`).
4. `DbExecutor` and `/health` use the resolved connection string unchanged.

Key Vault secret names use `--` for hierarchy, e.g. `AzureAd--ClientId` → `AzureAd:ClientId` (for a later Entra PR).

## One-time Azure setup after infra deploy

All LifeInsuranceCRM Azure resources deploy to subscription **`605a6796-5cf0-4a61-80f0-ff2d484360ee`**. `deploy-infra-dev.ps1` selects it automatically.

### 1. Redeploy infra (grants Key Vault Secrets User to the API identity)

```powershell
.\scripts\deploy-infra-dev.ps1
```

Note outputs (names are auto-generated on first deploy — copy from output, do not assume `licrm-dev-sql`):

- `containerAppName` (e.g. `licrm-dev-api`) — SQL Entra user name
- `sqlServerName`, `sqlServerFqdn` — for grant script and connection config
- `acrName`, `acrLoginServer` — for GitHub deploy workflow
- `containerAppIdentityPrincipalId` — managed identity object ID
- `keyVaultUri`, `keyVaultName`

### 2. Configure SQL Entra administrator (if not already set)

Set `sqlAzureAdAdministratorObjectId` in your `.bicepparam` to an Entra user or group that can create database users, then redeploy. Or assign manually in Azure Portal → SQL server → Microsoft Entra ID.

### 3. Grant the API managed identity access to the database

Run as the SQL Entra administrator:

```powershell
.\scripts\grant-api-sql-access.ps1 `
  -ResourceGroup rg-licrm-dev `
  -SqlServer <sqlServerName from deploy output> `
  -Database LifeInsuranceCRM `
  -ContainerAppName licrm-dev-api
```

This creates an Entra contained user mapped to the Container App identity and adds `db_datareader` / `db_datawriter`.

### 4. (Optional) Store secrets in Key Vault

```powershell
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--TenantId" --value "<tenant-id>"
```

Redeploy or restart the Container App after adding secrets the API reads at startup.

## Verify

```powershell
az containerapp logs show --name licrm-dev-api --resource-group rg-licrm-dev --follow
curl https://<containerAppFqdn>/health
```

Readiness should report SQL healthy once the Entra database user exists.

## Local development

No changes required:

- Run via Aspire AppHost (`ConnectionStrings:LifeInsuranceCRM`).
- Do not set `KeyVault:VaultUri` locally unless testing Key Vault integration explicitly.
