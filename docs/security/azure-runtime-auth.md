# Azure runtime auth (PR 3)

Production Container Apps authenticate to Azure SQL and Key Vault with the API's **system-assigned managed identity**. Local development continues to use Aspire, user secrets, and `appsettings.Development.json`. The API **does not** load production Key Vault from a laptop unless `KeyVault:AllowLocalAccess` is explicitly set after JIT/PIM elevation.

## How it works

| Environment | SQL auth | Secrets |
|-------------|----------|---------|
| Local (Aspire) | `ConnectionStrings:LifeInsuranceCRM` from AppHost | User secrets / appsettings. Key Vault is skipped. |
| Azure Container Apps | `Database:Server` + `Database:Name` → Active Directory Default | Key Vault via `KeyVault:VaultUri` + managed identity |

Startup order (`Program.cs`):

1. Read `KeyVault` options (`VaultUri`, `AllowLocalAccess`).
2. `KeyVaultConfiguration.Evaluate` decides skip / load / fail:
   - **Development** with no URI, or URI without managed identity / `AllowLocalAccess` → skip (Aspire + user secrets).
   - **Non-Development** with no URI → fail fast.
   - **Non-Development** with a URI but no managed identity and `AllowLocalAccess=false` → fail (blocks prod vault from laptops).
   - Managed identity or `AllowLocalAccess` → `AddAzureKeyVault` with `DefaultAzureCredential` (developer credentials excluded in Azure).
3. Resolve the effective SQL connection string (`DatabaseConnectionStringResolver`).
4. `DbExecutor` and `/health` use the resolved connection string unchanged.
5. Application Insights uses `APPLICATIONINSIGHTS_CONNECTION_STRING` or Key Vault `ApplicationInsights--ConnectionString`.

Key Vault secret names use `--` for hierarchy, e.g. `AzureAd--ClientId` → `AzureAd:ClientId`. Create the Entra app registrations and Conditional Access policies first — see [entra-policies.md](entra-policies.md).

## Secrets stored in Key Vault

| Secret name | Config key | When |
|-------------|------------|------|
| `AzureAd--TenantId` | `AzureAd:TenantId` | Always in Azure |
| `AzureAd--ClientId` | `AzureAd:ClientId` | Always in Azure |
| `AzureAd--Audience` | `AzureAd:Audience` | Always in Azure |
| `Database--ConnectionString` | `Database:ConnectionString` | Only if not using managed-identity SQL (1.4 is MI-only in prod) |
| `ApplicationInsights--ConnectionString` | `ApplicationInsights:ConnectionString` | Optional; Container Apps also inject `APPLICATIONINSIGHTS_CONNECTION_STRING` |

Field-level encryption (phase 2.1) uses the Key Vault **key** named by `KeyVault:FieldEncryptionKeyName` (default `field-encryption`). That is a cryptographic key, not a secret; create it in the vault before enabling envelope encryption.

Do not commit vault URIs, connection strings, or `AllowLocalAccess: true`.

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

### 4. Store Entra app settings in Key Vault

After the API app registration exists ([entra-policies.md](entra-policies.md)):

```powershell
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--TenantId" --value "<tenant-id>"
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--ClientId" --value "<api-client-id>"
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--Audience" --value "api://life-insurance-crm"
```

Optional — App Insights connection string (Bicep already injects the env var; this is a Key Vault backup):

```powershell
az keyvault secret set --vault-name <keyVaultName> --name "ApplicationInsights--ConnectionString" --value "<connection-string>"
```

Optional — SQL connection string **only** if managed-identity SQL is not in use:

```powershell
az keyvault secret set --vault-name <keyVaultName> --name "Database--ConnectionString" --value "<ado-net-connection-string>"
```

Redeploy or restart the Container App after adding secrets the API reads at startup.

## Verify

```powershell
az containerapp logs show --name licrm-dev-api --resource-group rg-licrm-dev --follow
curl https://<containerAppFqdn>/health
```

Readiness should report SQL healthy once the Entra database user exists. Startup fails fast if `KeyVault:VaultUri` is missing outside Development.

## Local development

No Key Vault for daily Aspire work:

- Run via Aspire AppHost (`ConnectionStrings:LifeInsuranceCRM`).
- Put non-Azure secrets in user secrets (`dotnet user-secrets`, id `life-insurance-crm-api-dev`).
- Leave `KeyVault:VaultUri` empty. A URI set in Development is ignored unless managed identity is present or `KeyVault:AllowLocalAccess` is true.

### JIT/PIM exception (rare)

To load a vault from a laptop after Privileged Identity Management elevation:

```powershell
az login
cd src/main
dotnet user-secrets set "KeyVault:VaultUri" "https://<vault-name>.vault.azure.net/"
dotnet user-secrets set "KeyVault:AllowLocalAccess" "true"
```

Use a **non-prod** vault unless the incident requires prod. Unset both secrets when finished:

```powershell
dotnet user-secrets remove "KeyVault:VaultUri"
dotnet user-secrets remove "KeyVault:AllowLocalAccess"
```
