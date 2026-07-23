# Security — local developer setup

## Pre-commit secret scanning

This repository blocks commits that contain likely secrets (API keys, connection strings with passwords, JWTs, private keys, and similar patterns).

### One-time setup

1. Install [pre-commit](https://pre-commit.com/#install) (Python 3.9+):

   ```bash
   pip install pre-commit
   ```

2. From the repository root (`life-insurance-crm-api`), install hooks:

   ```bash
   pre-commit install
   ```

3. Optional: run against all tracked files before your first commit:

   ```bash
   pre-commit run --all-files
   ```

### What runs on each commit

| Hook | Purpose |
|------|---------|
| **gitleaks** | Scans staged changes for hardcoded secrets (uses [`.gitleaks.toml`](../../.gitleaks.toml)) |
| **detect-private-key** | Blocks PEM private keys |
| **check-added-large-files** | Blocks accidental commits of files larger than 500 KB |

### Bypass (emergency only)

To skip gitleaks for a single commit (requires justification and team awareness):

```bash
SKIP=gitleaks git commit -m "your message"
```

Do not use bypass for real credentials. Rotate any secret that was ever committed, even if the commit was amended or reverted.

### CI backup

GitHub Actions runs the same gitleaks configuration on every push and pull request to `main`. CI catches issues if a developer commits without installing pre-commit locally.

### Adding allowlist entries

If gitleaks reports a false positive:

1. Confirm the value is not a real secret.
2. Add a targeted allowlist rule to [`.gitleaks.toml`](../../.gitleaks.toml) with a short description.
3. Avoid broad regexes that could hide actual leaks.

Development GUIDs (`11111111-…`, `22222222-…`, `00000000-…`) are already allowlisted.
