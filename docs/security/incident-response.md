# Compromise response

Use this when a person, the GitHub deploy identity, or the SQL password login may be in the wrong hands. Do it from a break-glass account, not from the account you suspect.

## Standing access

Daily Azure work uses an account that is not subscription Owner. The API runs as its managed identity (`db_datareader` and `db_datawriter` only). GitHub deploys as **BrokerBook GitHub Deployer** on the resource group. That role cannot delete the SQL server or database, delete long-term retention backups, purge Key Vault, delete Log Analytics or diagnostic settings, delete resource locks, or export the database.

These identities are separate, use MFA, and are not used for GitHub or the app:

- Subscription Owner. This is the only account that should remove a production `CanNotDelete` lock. The account that deploys or runs the app is not this Owner.
- SQL Entra administrator. This is not the same person as the everyday Owner, and not the GitHub identity.

Break-glass cloud accounts are also described in [entra-policies.md](entra-policies.md). They are excluded from Conditional Access, stored offline, and unused day to day.

Production locks sit on the SQL server, Key Vault, and Log Analytics workspace. Dev has no locks. Only a subscription Owner or User Access Administrator can remove a lock. Do not remove one to make a deploy succeed.

## Disable a person

1. Block sign-in and revoke existing sessions. Replace `<object-id>` with the Entra object id of the affected user.

   ```powershell
   az ad user update --id <object-id> --account-enabled false
   az rest --method POST --url "https://graph.microsoft.com/v1.0/users/<object-id>/revokeSignInSessions"
   ```

2. In the BrokerBook **Users** screen, mark that organization user inactive. The API then returns 403 even if a token is still presented. An Entra block alone does not update the CRM user row.

3. If that user can set Key Vault secrets, remove **Key Vault Secrets Officer** from the vault. Resource group Owner does not grant that data-plane role, so remove the assignment on the vault itself.

   ```powershell
   az role assignment delete --assignee-object-id <object-id> --role "Key Vault Secrets Officer" --scope <vault-resource-id>
   ```

4. If that user is the SQL Entra administrator, point the server at the break-glass admin before you remove the compromised one. Do not leave the server with no Entra administrator.

   ```powershell
   az sql server ad-admin create --resource-group rg-bbcrm-prod --server-name <sql-server> --display-name "<break-glass-upn>" --object-id <break-glass-object-id>
   ```

## Rotate database credentials

Reset the `sqladmin` password even when the compromised account was an Entra user. The password is a standing login until Entra-only authentication is on.

```powershell
az sql server update --resource-group rg-bbcrm-prod --name <sql-server> --admin-password "<new-password>"
```

Turn off SQL authentication only after the break-glass Entra administrator has connected successfully. Enabling it earlier can lock everyone out of the server.

```powershell
az sql server ad-only-auth enable --resource-group rg-bbcrm-prod --name <sql-server>
```

## GitHub deploy identity

If the pipeline identity or the GitHub repository may be compromised, remove **BrokerBook GitHub Deployer** from `rg-bbcrm-prod` (and `rg-bbcrm-dev` if that identity was used there). The identity name is `bbcrm-<env>-github-deploy`.

```powershell
az role assignment delete --assignee-object-id <deploy-identity-principal-id> --role "BrokerBook GitHub Deployer" --scope /subscriptions/<subscription-id>/resourceGroups/rg-bbcrm-prod
```

Leave the `CanNotDelete` locks in place. Restoring deploy access later is a local Owner run of `scripts/deploy-infra.ps1`, which recreates the role assignment. It does not grant subscription Owner to GitHub.

## After the account is contained

Check Entra sign-in logs and the production Administrative activity log in Log Analytics for role changes, encryption changes, lock deletes, and database exports. Those events also email `securityAlertEmail` when that address is set in `infra/parameters/prod.bicepparam`.

Do not delete backups, diagnostic settings, or the Key Vault as part of cleanup. A backup delete or a lock remove is a break-glass Owner action and only after you know the data is safe.
