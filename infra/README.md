# Azure infrastructure (Bicep)

Modular Bicep for LifeInsuranceCRM on Azure:

- VNet with Container Apps and private endpoint subnets
- Azure SQL (no public endpoint, private link, TDE, auditing; Geo backups + long-term retention in prod)
- Key Vault (RBAC, soft delete, private endpoint)
- Azure Container Registry (admin disabled)
- Container Apps Environment + API app (managed identity, health probes)
- Azure Static Web Apps for the Vite SPA (CORS origin wired into the API)
- Log Analytics + Application Insights
- GitHub Actions OIDC identities for **both** repos (API deploy vs client deploy; no long-lived SP secrets)

Infra **provisions and wires** the API and the client. Each GitHub repo **deploys its own app** into that platform.

## Layout

```text
infra/
  main.bicep                 # Entry point (resource group scope)
  modules/
    network.bicep
    monitor.bicep
    acr.bicep
    keyvault.bicep
    sql.bicep
    containerapps.bicep
    staticwebapp.bicep
    github-oidc.bicep
    github-client-oidc.bicep
    role-assignment.bicep
  parameters/
    dev.bicepparam
    prod.bicepparam
```

Container images are built from [`src/Dockerfile`](../src/Dockerfile).

## Cost-conscious defaults (startup)

Sizing is parameterized per environment. Defaults target the **lowest viable compute** while keeping the security layout (private SQL/Key Vault, RBAC, OIDC deploy).

| Resource | Dev | Prod |
|----------|-----|------|
| Container App CPU / memory | 0.25 vCPU / 0.5 GiB | 0.5 vCPU / 1 GiB |
| Container App replicas | 0–1 (scale to zero when idle) | 1–2 (always at least one) |
| Azure SQL | Serverless GP_S_Gen5 (auto-pause after 60 min idle) | Basic (~$5/mo; bump to S0/Standard when needed) |
| ACR | Basic | Basic |
| Static Web App | Free | Free |
| Log Analytics retention | 30 days (PerGB2018 minimum) | 30 days |
| SQL backups | Local redundancy, no LTR | Geo redundancy + 4 weeks / 12 months / 5 years LTR |
| SQL auditing / diagnostics | Off (saves ingestion) | On |

Override any value in `infra/parameters/*.bicepparam` or at deploy time, e.g. `--parameters sqlSkuName=S0 sqlSkuTier=Standard`.

**Note:** If an environment already deployed with Standard SQL, downgrading to Basic in Bicep may require a manual SKU change or database recreate — plan tier changes during maintenance.

## Prerequisites

1. Azure subscription and `az` CLI logged in
2. **Canonical subscription:** `605a6796-5cf0-4a61-80f0-ff2d484360ee` (Primary). The deploy script switches to this subscription automatically.
3. Resource group per environment, e.g. `rg-bbcrm-dev` (can be in any region; resources use the `location` parameter)
4. GitHub repository environments: `dev`, `prod`

SQL server, ACR, and Key Vault names are **globally unique** across all of Azure. Bicep generates names from the subscription + resource group ID so a new `rg-bbcrm-dev` never collides with resources in another subscription. When migrating existing servers/registries into this subscription, set `sqlServerNameOverride` / `acrNameOverride` in the parameter file.

### Pick a region where Azure SQL is allowed

Some subscriptions block SQL in certain regions (e.g. `eastus2` on many accounts). Check before deploying:

```powershell
az sql db list-editions --location centralus --query "[?name=='Standard'].reason" -o tsv
```

If output is empty, that region works. Also verified for this subscription: `centralus`, `southcentralus`, `westus2`.

Default parameter files use **`centralus`**. Override at deploy time if needed:

```powershell
--parameters location=westus2
```

## First-time deploy (local)

```powershell
az group create --name rg-bbcrm-dev --location centralus
```

**Safer password passing** — `az` does not accept a JSON `@parameters` file together with a `.bicepparam` file. Use the helper script (recommended):

```powershell
.\scripts\deploy-infra-dev.ps1
```

Or create a one-off `.bicepparam` with the password filled in (do not commit):

```powershell
Copy-Item infra/parameters/dev.bicepparam infra/parameters/dev.local.bicepparam
# Edit dev.local.bicepparam: set sqlAdministratorLoginPassword to a strong value (do not commit)

az deployment group create `
  --resource-group rg-bbcrm-dev `
  --template-file infra/main.bicep `
  --parameters infra/parameters/dev.local.bicepparam

Remove-Item infra/parameters/dev.local.bicepparam
```

After deploy, note outputs:

- `githubDeployClientId` — federated identity client ID for the **API** GitHub repo
- `githubClientDeployClientId` — federated identity client ID for the **client** GitHub repo
- `acrLoginServer`, `containerAppFqdn`, `keyVaultUri`, `keyVaultName`, `sqlServerFqdn`
- `clientOrigin`, `clientRedirectUri`, `staticWebAppName` — SPA URL and Entra redirect URI

### Key Vault name already in use (`VaultAlreadyExists`)

Key Vault names are **global**. Deleting the resource group soft-deletes the vault; with purge protection the old name can be locked for up to 90 days.

Check soft-deleted vaults:

```powershell
az keyvault list-deleted --query "[?contains(name, 'bbcrm')]" -o table
```

Bicep now generates a unique vault name per resource group instance (hash uses the resource group **ID**, not its name, so recreating `rg-bbcrm-dev` gets a new vault name). Purge protection stays **enabled** (Azure does not allow turning it off once set).

If purge protection is **disabled** on a deleted vault, you can purge manually:

```powershell
az keyvault purge --name bbcrm-dev-kv --location eastus2
```

### If a previous deploy failed partway

A failed run may leave some resources in the resource group. Re-run the same deploy command; Bicep will retry/create missing resources incrementally. If deployment state is stuck, check operations:

```powershell
az deployment group list --resource-group rg-bbcrm-dev -o table
az resource list --resource-group rg-bbcrm-dev -o table
```

## GitHub Actions setup

Create a GitHub **environment** matching the target (`dev` or `prod`) in **each** repository and configure secrets:

**API repo** (`life-insurance-crm-api`):

| Secret | Source |
|--------|--------|
| `AZURE_CLIENT_ID` | Bicep output `githubDeployClientId` |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| `SQL_ADMIN_PASSWORD` | Strong bootstrap password (infra deploy only) |

To keep Key Vault secret access after GitHub redeploys, set `keyVaultSecretsOfficerPrincipalId` in `infra/parameters/<env>.bicepparam` to your Entra object ID (`az ad signed-in-user show --query id -o tsv`). That value is not a secret. Until it is set, run [`scripts/grant-keyvault-secrets-officer.ps1`](../scripts/grant-keyvault-secrets-officer.ps1) once on the live vault.

**Client repo** (`life-insurance-crm-client`):

| Secret | Source |
|--------|--------|
| `AZURE_CLIENT_ID` | Bicep output `githubClientDeployClientId` (not the API identity) |
| `AZURE_TENANT_ID` | Same tenant as above |
| `AZURE_SUBSCRIPTION_ID` | Same subscription as above |

The GitHub **environment** name (`dev` or `prod`) must match the workflow input and the OIDC federated credential.

### Workflows

| Workflow | Repo | Purpose |
|----------|------|---------|
| [`deploy-infrastructure.yml`](../.github/workflows/deploy-infrastructure.yml) | API | Manual Bicep deploy / update of the whole platform |
| [`deploy-api.yml`](../.github/workflows/deploy-api.yml) | API | Build Docker image, push to ACR by digest, update Container App |
| `deploy-client.yml` | Client | Build Vite SPA, upload to the provisioned Static Web App |

Both deploy workflows use OIDC (`azure/login@v2`) — no client secrets in GitHub. The client identity can update the Static Web App and read the API FQDN; it cannot change SQL, Key Vault, or the Container App image.

Typical order:

1. Run **Deploy infrastructure** from the API repo
2. Configure GitHub environment secrets in **both** repos from deployment outputs
3. Add `clientRedirectUri` to the Entra SPA registration (see [`docs/security/entra-policies.md`](../docs/security/entra-policies.md))
4. Run **Deploy API** to push the real API image
5. Run **Deploy client** from the client repo (sets `VITE_API_BASE_URL` from the API FQDN)

Bicep sets API `Cors:AllowedOrigins` to `clientOrigin` so the SPA can call the API once both apps are deployed.

## Security notes

**PR 3 (API runtime auth)** wires Key Vault configuration and managed-identity SQL in the API. See [`docs/security/azure-runtime-auth.md`](../docs/security/azure-runtime-auth.md).

After deploying infra:

1. Run [`scripts/grant-api-sql-access.ps1`](../scripts/grant-api-sql-access.ps1) to map the Container App identity to the SQL database.
2. Run [`scripts/grant-keyvault-secrets-officer.ps1`](../scripts/grant-keyvault-secrets-officer.ps1) so you can set vault secrets (RG Owner is not enough). Then create Entra app registrations and store `AzureAd--*` secrets. See [`docs/security/entra-policies.md`](../docs/security/entra-policies.md) and [`docs/security/azure-runtime-auth.md`](../docs/security/azure-runtime-auth.md).
3. Deploy the API image via GitHub Actions.

Infra grants the API managed identity **Key Vault Secrets User** (read) and **AcrPull**. Humans who set secrets need **Key Vault Secrets Officer**. SQL still needs the one-time Entra database user script above.

Remaining follow-ups:

- Optional CMK for SQL TDE
- Retire bootstrap SQL login once Entra-only admin is verified

## Validate templates locally

```bash
az bicep build --file infra/main.bicep
```
