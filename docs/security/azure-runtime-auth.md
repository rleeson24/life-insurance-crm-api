# Azure runtime auth (PR 3)

Production Container Apps authenticate to Azure SQL and Key Vault with the API's **system-assigned managed identity**. Local development continues to use Aspire, user secrets, and `appsettings.Development.json`. The API **does not** load production Key Vault from a laptop unless `KeyVault:AllowLocalAccess` is explicitly set after JIT/PIM elevation.

## How it works

| Environment | SQL auth | Secrets |
|-------------|----------|---------|
| Local (Aspire) | `ConnectionStrings:BrokerBook` from AppHost | User secrets / appsettings. Key Vault is skipped. |
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
| `FieldEncryption--Key` | `FieldEncryption:Key` | Azure: 32-byte AES DEK as base64. Local Development uses a built-in non-prod DEK if this is empty. |
| `FieldEncryption--WrappedDek` | `FieldEncryption:WrappedDek` | Optional. RSA-OAEP-256 wrap of the same 32-byte DEK using the vault key `field-encryption`. Preferred over a raw DEK secret. |
| `FieldEncryption--BlindIndexKey` | `FieldEncryption:BlindIndexKey` | Azure: 32-byte HMAC key as base64 for Medicare number blind indexes. Must be distinct from the DEK. Local Development uses a built-in non-prod key if empty. |

Field-level encryption (phase 2.1) encrypts `MedicareNumber`, `DateOfBirth`, `MedicarePartAEffectiveDate`, and `MedicarePartBEffectiveDate` in the API before SQL writes. Those columns are `varbinary(max)` (`012_ClientFieldEncryption.sql`). Name, phone, and email stay searchable plaintext.

Medicare number **search** uses a keyed HMAC blind index in `MedicareNumberBlindIndex` (same script). The API normalizes the query (uppercase, strip dashes) and looks up the 32-byte hash. The index is not reversible to the MBI.

- **Key Vault RSA key** named by `KeyVault:FieldEncryptionKeyName` (default `field-encryption`) wraps the DEK. Bicep creates this key; the API identity needs **Key Vault Crypto User** to unwrap.
- **Raw DEK secret** `FieldEncryption--Key` is the simpler Azure bootstrap: a 32-byte key, base64-encoded. The API never logs this value.
- **Blind index key** `FieldEncryption--BlindIndexKey` is a separate 32-byte HMAC key. Re-saving clients backfills `MedicareNumberBlindIndex` on existing databases.
- **Local Aspire:** if `FieldEncryption:Key` is unset, the API derives a stable Development-only DEK. Do not use that material in Azure.

Apply `012_ClientFieldEncryption.sql` to existing databases before deploying the API that writes ciphertext and blind indexes. Existing plaintext in encrypted columns is discarded (T-SQL cannot encrypt without the DEK). Re-enter DOB, MBI, and Part A/B dates in the UI if needed.

Do not commit vault URIs, connection strings, or `AllowLocalAccess: true`.

## One-time Azure setup after infra deploy

All BrokerBook Azure resources deploy to subscription **`605a6796-5cf0-4a61-80f0-ff2d484360ee`**. `deploy-infra-dev.ps1` selects it automatically.

### 1. Redeploy infra (grants Key Vault Secrets User to the API identity)

```powershell
.\scripts\deploy-infra-dev.ps1
```

Note outputs (names are auto-generated on first deploy — copy from output, do not assume `bbcrm-dev-sql`):

- `containerAppName` (e.g. `bbcrm-dev-api`) — SQL Entra user name
- `sqlServerName`, `sqlServerFqdn` — for grant script and connection config
- `acrName`, `acrLoginServer` — for GitHub deploy workflow
- `containerAppIdentityPrincipalId` — managed identity object ID
- `keyVaultUri`, `keyVaultName`
- `clientOrigin`, `clientRedirectUri`, `staticWebAppName` — SPA URL; add `clientRedirectUri` to the Entra SPA registration
- `githubDeployClientId` — API repo GitHub OIDC (`AZURE_CLIENT_ID`)
- `githubClientDeployClientId` — client repo GitHub OIDC (`AZURE_CLIENT_ID` in **that** repo)

CORS `Cors:AllowedOrigins` is set from `clientOrigin` on the Container App. After both apps are deployed, the browser origin matches the API allow list.

### 2. Configure SQL Entra administrator (if not already set)

Set `sqlAzureAdAdministratorObjectId` in your `.bicepparam` to an Entra user or group that can create database users, then redeploy. Or assign manually in Azure Portal → SQL server → Microsoft Entra ID.

### 3. Grant the API managed identity access to the database

Run as the SQL Entra administrator (the script sets you as Entra admin if none exists, then creates the database user):

```powershell
.\scripts\grant-api-sql-access.ps1 -ResourceGroup rg-bbcrm-dev
```

If the SQL server is private-endpoint only, the script temporarily enables public access from your IP, runs T-SQL, then turns public access off again.

This creates an Entra contained user mapped to the Container App identity and adds `db_datareader` / `db_datawriter`.

### 4. Grant yourself Key Vault Secrets Officer

The vault uses **RBAC** (`enableRbacAuthorization: true`). Resource group Owner/Contributor is **control plane only** — it does not let you list, read, or set secrets. Infra grants **Key Vault Secrets User** to the API managed identity (runtime read). A person who sets secrets needs **Key Vault Secrets Officer**.

For an existing vault (no full redeploy):

```powershell
.\scripts\grant-keyvault-secrets-officer.ps1 -ResourceGroup rg-bbcrm-dev
```

That assigns Secrets Officer to the signed-in user. Wait one to two minutes, then refresh the portal. Future local deploys (`deploy-infra-dev.ps1`) pass your object ID automatically. To keep the assignment in Bicep, set `keyVaultSecretsOfficerPrincipalId` in `infra/parameters/dev.bicepparam` (`az ad signed-in-user show --query id -o tsv`). That GUID is not a secret.

The vault is private-endpoint only. After RBAC works, a laptop can still hit a **firewall** error. Temporarily enable public access, set secrets, then disable it again (the grant script prints those commands).

### 5. Store Entra app settings in Key Vault

After the API app registration exists ([entra-policies.md](entra-policies.md)):

```powershell
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--TenantId" --value "<tenant-id>"
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--ClientId" --value "6c970234-fee3-4568-97d8-7d015c903368"
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--Audience" --value "api://6c970234-fee3-4568-97d8-7d015c903368"
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
az containerapp logs show --name bbcrm-dev-api --resource-group rg-bbcrm-dev --follow
curl https://<containerAppFqdn>/health
```

Readiness should report SQL healthy once the Entra database user exists. Startup fails fast if `KeyVault:VaultUri` is missing outside Development.

## Local development

No Key Vault for daily Aspire work:

- Run via Aspire AppHost (`ConnectionStrings:BrokerBook`).
- Put non-Azure secrets in user secrets (`dotnet user-secrets` on the BrokerBook API project).
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
