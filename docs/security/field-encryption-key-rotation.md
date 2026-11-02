# Field encryption key rotation, recovery, and revocation

Production field encryption uses two Key Vault secrets and one RSA key. This runbook is the SS-KEY-ROTATION procedure. Bootstrap of the secrets is in [azure-runtime-auth.md](azure-runtime-auth.md).

| Material | Vault object | What it protects |
| --- | --- | --- |
| Data key | Secret `FieldEncryption--Key` | Medicare number, date of birth, Medicare Part A date, Medicare Part B date |
| Blind-index key | Secret `FieldEncryption--BlindIndexKey` | Medicare number search hash only. It cannot decrypt those fields |
| Wrap key | RSA key `field-encryption` | Used only when `FieldEncryption--WrappedDek` is set. The API uses `FieldEncryption--Key` when that secret is present and ignores the wrapped value |

The API loads the latest secret version at startup and keeps one data key in memory. Ciphertext is stamped with key version **1**. Decrypt accepts only that version. There is no job that re-encrypts existing rows.

**Do not replace `FieldEncryption--Key`, `FieldEncryption--BlindIndexKey`, or `FieldEncryption:KeyVersion` on a database that already has ciphertext.** A new secret or a new version number makes existing Medicare numbers, dates of birth, and Part A/B dates fail to decrypt. A new blind-index key makes Medicare number search miss until each client is saved again. Replacing a leaked data key requires decrypting every protected field and writing it again, which this API cannot do in one pass.

SQL Transparent Data Encryption is service-managed by Microsoft and is not this procedure.

The production vault is private, soft-delete retention is 90 days, and purge protection is on. Do not purge secrets or keys. Listing and restoring secret versions requires **Key Vault Secrets Officer**. Do not print secret values.

## Inventory

From `life-insurance-crm-api`:

```powershell
.\scripts\list-field-encryption-key-versions.ps1 -ResourceGroup rg-bbcrm-prod
```

The script turns public network access on, allows this machine's IP, prints version id, enabled, and created time for both secrets, then turns public access off and removes the IP rule. It does not read secret values. Reading the `field-encryption` RSA key needs a key role such as Key Vault Crypto User. Secrets Officer does not include that. A missing key role is a warning. The secret inventory still succeeds, and the wrap key is unused while `FieldEncryption--Key` is enabled.

Record the version ids and created times in the security checklist evidence log. Leave the values out.

## Recovery

Key Vault keeps older secret versions. `az keyvault secret set` adds a version. It does not erase the previous one. The app reads the latest version only after the Container App restarts.

If a bad value was published, copy the last good version into a new current version and restart the API. Clear the shell variable afterward.

```powershell
.\scripts\list-field-encryption-key-versions.ps1 -ResourceGroup rg-bbcrm-prod
$previous = az keyvault secret show --vault-name <vaultName> --name FieldEncryption--Key --version <good-version-id> --query value -o tsv
az keyvault secret set --vault-name <vaultName> --name FieldEncryption--Key --value $previous
Remove-Variable previous
az containerapp revision restart --name bbcrm-prod-api --resource-group rg-bbcrm-prod
```

`secret show` is a data-plane call. Run it while public access is still enabled, or use the same public-access window the inventory script opens. Do not write the value to a file or the evidence log.

Use the same commands for `FieldEncryption--BlindIndexKey` when that secret was the one published in error. A wrong blind-index key does not block decrypt.

If the secret was deleted, recover it. Do not purge it.

```powershell
az keyvault secret recover --vault-name <vaultName> --name FieldEncryption--Key
```

If `FieldEncryption:KeyVersion` was changed from 1, set it back to 1 before restarting. Existing ciphertext is stamped version 1, and the API rejects any other version.

## Revocation

Removing access does not decrypt or destroy ciphertext.

| Action | Effect |
| --- | --- |
| Remove **Key Vault Secrets Officer** from a person on this vault | That person can no longer list, read, or set secrets |
| Remove **Key Vault Secrets User** from `bbcrm-prod-api` | The next start cannot load the keys. The running process keeps the key until restart |
| Disable the current secret version | The next start cannot load that version. Disable a version only when a copy that can still decrypt the rows is retained |

A leaked data key is an incident. Restrict vault access and keep the current key so the data remains readable. Schedule a maintenance re-encrypt before the current key is disabled. Until the app can decrypt version 1 and write a new version in one pass, do not disable the version that encrypted the live rows.
