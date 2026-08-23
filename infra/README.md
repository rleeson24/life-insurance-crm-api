# Azure infrastructure (Bicep)

Modular Bicep for LifeInsuranceCRM on Azure:

- VNet with Container Apps and private endpoint subnets
- Azure SQL (no public endpoint, private link, TDE, auditing)
- Key Vault (RBAC, soft delete, private endpoint)
- Azure Container Registry (admin disabled)
- Container Apps Environment + API app (managed identity, health probes)
- Log Analytics + Application Insights
- GitHub Actions OIDC deploy identity (no long-lived SP secrets)

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
    github-oidc.bicep
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
| Log Analytics retention | 30 days (PerGB2018 minimum) | 30 days |
| SQL auditing / diagnostics | Off (saves ingestion) | On |

Override any value in `infra/parameters/*.bicepparam` or at deploy time, e.g. `--parameters sqlSkuName=S0 sqlSkuTier=Standard`.

**Note:** If an environment already deployed with Standard SQL, downgrading to Basic in Bicep may require a manual SKU change or database recreate — plan tier changes during maintenance.

## Prerequisites

1. Azure subscription and `az` CLI logged in
2. **Canonical subscription:** `605a6796-5cf0-4a61-80f0-ff2d484360ee` (Primary). The deploy script switches to this subscription automatically.
3. Resource group per environment, e.g. `rg-licrm-dev` (can be in any region; resources use the `location` parameter)
4. GitHub repository environments: `dev`, `prod`

SQL server, ACR, and Key Vault names are **globally unique** across all of Azure. Bicep generates names from the subscription + resource group ID so a new `rg-licrm-dev` never collides with resources in another subscription. When migrating existing servers/registries into this subscription, set `sqlServerNameOverride` / `acrNameOverride` in the parameter file.

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
az group create --name rg-licrm-dev --location centralus
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
  --resource-group rg-licrm-dev `
  --template-file infra/main.bicep `
  --parameters infra/parameters/dev.local.bicepparam

Remove-Item infra/parameters/dev.local.bicepparam
```

After deploy, note outputs:

- `githubDeployClientId` — federated identity client ID for GitHub Actions
- `acrLoginServer`, `containerAppFqdn`, `keyVaultUri`, `keyVaultName`, `sqlServerFqdn`

### Key Vault name already in use (`VaultAlreadyExists`)

Key Vault names are **global**. Deleting the resource group soft-deletes the vault; with purge protection the old name can be locked for up to 90 days.

Check soft-deleted vaults:

```powershell
az keyvault list-deleted --query "[?contains(name, 'licrm')]" -o table
```

Bicep now generates a unique vault name per resource group instance (hash uses the resource group **ID**, not its name, so recreating `rg-licrm-dev` gets a new vault name). Purge protection stays **enabled** (Azure does not allow turning it off once set).

If purge protection is **disabled** on a deleted vault, you can purge manually:

```powershell
az keyvault purge --name licrm-dev-kv --location eastus2
```

### If a previous deploy failed partway

A failed run may leave some resources in the resource group. Re-run the same deploy command; Bicep will retry/create missing resources incrementally. If deployment state is stuck, check operations:

```powershell
az deployment group list --resource-group rg-licrm-dev -o table
az resource list --resource-group rg-licrm-dev -o table
```

## GitHub Actions setup

Create a GitHub **environment** matching the target (`dev` or `prod`) and configure secrets:

| Secret | Source |
|--------|--------|
| `AZURE_CLIENT_ID` | Bicep output `githubDeployClientId` |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| `SQL_ADMIN_PASSWORD` | Strong bootstrap password (infra deploy only) |

The GitHub **environment** name (`dev` or `prod`) must match the workflow input and the OIDC federated credential.

### Workflows

| Workflow | Purpose |
|----------|---------|
| [`deploy-infrastructure.yml`](../.github/workflows/deploy-infrastructure.yml) | Manual Bicep deploy / update |
| [`deploy-api.yml`](../.github/workflows/deploy-api.yml) | Build Docker image, push to ACR, update Container App |

Both use OIDC (`azure/login@v2`) — no client secrets in GitHub.

Typical order:

1. Run **Deploy infrastructure** for the environment
2. Configure GitHub environment secrets from deployment outputs
3. Run **Deploy API** to push the real API image (replaces the placeholder hello-world image)

## Security notes

**PR 3 (API runtime auth)** wires Key Vault configuration and managed-identity SQL in the API. See [`docs/security/azure-runtime-auth.md`](../docs/security/azure-runtime-auth.md).

After deploying infra:

1. Run [`scripts/grant-api-sql-access.ps1`](../scripts/grant-api-sql-access.ps1) to map the Container App identity to the SQL database.
2. Optionally populate Key Vault secrets (`AzureAd--TenantId`, etc.) for a later Entra PR.
3. Deploy the API image via GitHub Actions.

Infra grants the API managed identity **Key Vault Secrets User** and **AcrPull**. SQL still needs the one-time Entra database user script above.

Remaining follow-ups:

- Entra ID app registration values in Container App settings / Key Vault
- Optional CMK for SQL TDE
- Retire bootstrap SQL login once Entra-only admin is verified

## Validate templates locally

```bash
az bicep build --file infra/main.bicep
```
