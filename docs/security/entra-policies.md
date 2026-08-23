# Entra ID, MFA, and GitHub access policies

MFA and Conditional Access are **tenant settings**, not application code. The API validates Entra JWTs when `Auth:UseDevelopmentAuthentication` is `false` (`AddMicrosoftIdentityWebApi` in `Program.cs`). Local Aspire development keeps the synthetic auth handler.

**License:** Conditional Access requires [Microsoft Entra ID P1](https://learn.microsoft.com/entra/identity/conditional-access/overview) (or P2 / Microsoft 365 E3+). If the tenant still uses **Security defaults**, turn those off only after the named policies below are on — both cannot be active at once.

## Policy matrix

| ID | Policy | State | Applies to |
|----|--------|-------|------------|
| CA-MFA-ALL | Require MFA for all users | **Required now** | Azure portal, Microsoft 365, GitHub Entra SSO (when used), all cloud apps |
| CA-BLOCK-LEGACY | Block legacy authentication | **Required now** | Exchange ActiveSync, IMAP, SMTP AUTH, other clients that cannot do MFA |
| CA-MFA-AZURE | Require MFA for Azure management | **Required now** | Microsoft Azure Management (ARM / portal / CLI / PowerShell) |
| CA-COMPLIANT-ADMIN | Require compliant or Microsoft Entra hybrid joined device for privileged roles | **Phase 2** (optional) | Global Administrator, Privileged Role Administrator, Owners of prod resource groups |

Create these in **Microsoft Entra admin center → Protection → Conditional Access**. Assign to **All users**. Exclude only the documented break-glass accounts (see below). Do not exclude admin roles from MFA.

### CA-MFA-ALL — MFA for every interactive sign-in

1. New policy → name `CA-MFA-ALL`.
2. Users: **All users**. Exclude break-glass accounts only.
3. Target resources: **All cloud apps**.
4. Grant: **Require multifactor authentication**.
5. Enable: **On**.

This covers Azure Portal, prod resource groups, and GitHub if the org uses Entra SSO. It does not replace GitHub account 2FA (see [GitHub](#github-2fa-and-branch-protection)).

### CA-BLOCK-LEGACY — block legacy auth

1. New policy → name `CA-BLOCK-LEGACY`.
2. Users: **All users**.
3. Target resources: **All cloud apps**.
4. Conditions → Client apps: configure **Yes**; select **Exchange ActiveSync clients** and **Other clients**.
5. Grant: **Block access**.
6. Enable: **On**.

### CA-MFA-AZURE — Azure control plane

1. New policy → name `CA-MFA-AZURE`.
2. Users: **All users** (exclude break-glass only).
3. Target resources: **Microsoft Azure Management**.
4. Grant: **Require multifactor authentication**.
5. Enable: **On**.

Redundant with CA-MFA-ALL if that policy already targets all cloud apps. Keep it as a second control so Azure Portal / ARM cannot accidentally drop out of a future “all apps” exception.

### CA-COMPLIANT-ADMIN — privileged device compliance (phase 2)

Defer until Intune (or equivalent) device compliance is in place. When enabled:

- Users: directory roles **Global Administrator**, **Privileged Role Administrator**, plus members of the prod resource-group Owner group.
- Target resources: **Microsoft Azure Management**.
- Grant: **Require device to be marked as compliant** (and/or **Require Microsoft Entra hybrid joined device**).
- Session: **Sign-in frequency** 8 hours (aligns with later PIM JIT windows).

Do not enable this policy until at least one admin device is enrolled and compliant, or you will lock operators out of Azure.

### Break-glass accounts

Keep **one or two** cloud-only emergency accounts:

- Excluded from Conditional Access (and later from PIM).
- Long random passwords stored offline (not in this repo, not in Key Vault used by the app).
- No daily use, no mail client, no GitHub.
- Monitor sign-ins in Entra → Monitoring → Sign-in logs.

## App registrations (separate API and SPA)

Do **not** use one registration for both the API and the React client. The API is a resource that validates tokens; the SPA is a public client that requests them.

| App | Account type | Secret | Purpose |
|-----|--------------|--------|---------|
| `LifeInsuranceCRM-API` | Single tenant | None (JWT validation only) | Audience for access tokens |
| `LifeInsuranceCRM-SPA` | Single tenant | **None** (public SPA + PKCE) | MSAL login in the React app |

Supported account types: **Accounts in this organizational directory only**.

### 1. API registration (`LifeInsuranceCRM-API`)

Entra admin center → **App registrations** → **New registration**.

1. **Expose an API**
   - Application ID URI: `api://life-insurance-crm` (or `api://<api-application-client-id>`).
   - Add scope `access_as_user`:
     - Who can consent: **Admins and users**
     - Admin consent display name: `Access LifeInsuranceCRM API`
     - User consent display name: `Access the CRM as you`
2. **Token configuration** → optional claims on the **Access** token: `email`, `preferred_username`.  
   `ActorResolutionMiddleware` maps `oid` (always present) to `OrganizationUsers.UserId`, and email from `email` or `preferred_username`.
3. **Authentication**: no SPA or public-client platform on this app. Implicit grant stays **off**.
4. Copy **Application (client) ID** and **Directory (tenant) ID**.

Map to API configuration (`AzureAd` section — store in Key Vault in Azure, never commit values):

| Config key | Key Vault secret | Value |
|------------|------------------|-------|
| `AzureAd:Instance` | `AzureAd--Instance` | `https://login.microsoftonline.com/` |
| `AzureAd:TenantId` | `AzureAd--TenantId` | Directory (tenant) ID |
| `AzureAd:ClientId` | `AzureAd--ClientId` | **API** application (client) ID |
| `AzureAd:Audience` | `AzureAd--Audience` | Application ID URI (`api://life-insurance-crm`) |

`AzureAd:ClientId` is the **API** registration, not the SPA. After creating the secrets:

```powershell
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--TenantId" --value "<tenant-id>"
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--ClientId" --value "<api-client-id>"
az keyvault secret set --vault-name <keyVaultName> --name "AzureAd--Audience" --value "api://life-insurance-crm"
```

Restart the Container App so it reloads configuration. Runtime wiring is in [azure-runtime-auth.md](azure-runtime-auth.md).

### 2. SPA registration (`LifeInsuranceCRM-SPA`)

1. **New registration** → name `LifeInsuranceCRM-SPA` → single tenant.
2. **Authentication** → **Add a platform** → **Single-page application**:
   - `http://localhost:5387/` (Vite port in `life-insurance-crm-client/src/vite.config.ts`)
   - Production SPA origin when it exists (exact origin, trailing slash as Entra requires)
3. Implicit grant and hybrid flows: **off**. Auth code + PKCE only (MSAL default).
4. **API permissions** → **Add a permission** → **My APIs** → `LifeInsuranceCRM-API` → delegated `access_as_user` → **Grant admin consent**.
5. No client secret. No certificates.

MSAL in the client (phase 3.3) will use:

| Value | Source |
|-------|--------|
| SPA client ID | SPA application (client) ID |
| Tenant ID | Same directory as the API |
| API scope | `api://life-insurance-crm/access_as_user` |
| Redirect URI | Must match a URI registered above |

Until MSAL is wired, `life-insurance-crm-client/src/src/auth/auth.ts` remains a placeholder and local runs use development authentication.

### User provisioning

JWT `oid` must match `OrganizationUsers.UserId` or the API returns 403 (`TenantAccessDenied`). After a person is created in Entra:

1. Copy their **Object ID**.
2. Insert an `OrganizationUsers` row for the correct `TenantId` with that `UserId`, email, display name, and role.
3. Automated JIT provisioning is not built yet.

## GitHub 2FA and branch protection

Repos today:

| Repository | GitHub |
|------------|--------|
| API | [rleeson24/life-insurance-crm-api](https://github.com/rleeson24/life-insurance-crm-api) |
| Client | [rleeson24/life-insurance-crm-client](https://github.com/rleeson24/life-insurance-crm-client) |

These are **personal** repositories. There is no GitHub organization SSO yet. Enable account 2FA now; when an organization is created, require 2FA for the org and optionally Entra SSO.

### Account / org 2FA

1. GitHub → **Settings** → **Password and authentication** → enable 2FA (TOTP or passkey). Do not use SMS as the only factor.
2. When a GitHub **organization** exists: **Organization settings** → **Authentication security** → **Require two-factor authentication** for everyone.
3. **No shared admin accounts.** Each person uses their own GitHub identity and their own Entra identity. Do not share a PAT, `gh` login, or Azure login.

### Branch protection on `main`

Apply to **both** repositories. Direct pushes, force pushes, and deleting `main` are not allowed. Merges go through a pull request whose CI checks are green.

Required status checks (job names from `.github/workflows/ci.yml`):

| Repository | Required checks |
|------------|-----------------|
| `life-insurance-crm-api` | `secret-scan`, `vulnerability-scan`, `build-and-test` |
| `life-insurance-crm-client` | `secret-scan`, `vulnerability-scan`, `build` |

**Portal:** each repo → **Settings** → **Rules** → **Rulesets** → **New branch ruleset**:

- Enforcement: **Active**
- Target: `main`
- Restrict deletions
- Block force pushes
- Require a pull request before merging
- Required approvals: **0** while this is a solo maintainer (still requires a PR). Raise to **1** when a second person can review.
- Require status checks to pass — add the jobs in the table above
- Require conversation resolution before merging

**CLI** (after `gh auth login`), API repo:

```powershell
$payload = @'
{
  "name": "protect-main",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": { "include": ["refs/heads/main"], "exclude": [] }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews_on_push": true,
        "required_review_thread_resolution": true,
        "require_code_owner_review": false,
        "require_last_push_approval": false
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "do_not_enforce_on_create": false,
        "required_status_checks": [
          { "context": "secret-scan" },
          { "context": "vulnerability-scan" },
          { "context": "build-and-test" }
        ]
      }
    }
  ]
}
'@
$payload | gh api --method POST repos/rleeson24/life-insurance-crm-api/rulesets --input -
```

Repeat for `life-insurance-crm-client`, replacing the three `context` values with `secret-scan`, `vulnerability-scan`, and `build`.

Private personal repositories may need GitHub Pro for rulesets. If the API returns 403, use **Settings** → **Branches** → **Add classic branch protection rule** with the same checks.

### GitHub Environment `prod`

Deploy workflows use GitHub Environments `dev` and `prod`. For `prod`:

1. Repo → **Settings** → **Environments** → `prod`
2. **Required reviewers**: at least one person (when a second maintainer exists)
3. **Deployment branches**: `main` only

`dev` can stay unrestricted for iteration.

## Verification checklist

- [ ] Entra ID P1 (or Security defaults still covering MFA until P1 + CA are live)
- [ ] CA-MFA-ALL on; break-glass excluded and unused
- [ ] CA-BLOCK-LEGACY on
- [ ] CA-MFA-AZURE on
- [ ] `LifeInsuranceCRM-API` and `LifeInsuranceCRM-SPA` are **two** registrations; SPA has no secret
- [ ] API scope `api://life-insurance-crm/access_as_user` exists; admin consent granted
- [ ] Key Vault has `AzureAd--TenantId`, `AzureAd--ClientId`, `AzureAd--Audience`
- [ ] GitHub account 2FA enabled; no shared admins
- [ ] `main` ruleset active on both repos with the CI jobs required
- [ ] Test sign-in (after MSAL): token `aud` is `api://life-insurance-crm`, `oid` matches `OrganizationUsers.UserId`
