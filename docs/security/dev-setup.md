# Security — local developer setup

## Pre-commit secret scanning

This repository blocks commits that contain likely secrets (API keys, connection strings with passwords, JWTs, private keys, and similar patterns).

### One-time setup

**Windows (recommended):** from the repository root (`life-insurance-crm-api`):

```powershell
.\scripts\setup-pre-commit.ps1
```

**Manual setup (all platforms):**

1. Install [pre-commit](https://pre-commit.com/#install) (Python 3.9+):

   ```bash
   python -m pip install pre-commit
   ```

2. Install git hooks from the repository root:

   ```bash
   python -m pre_commit install
   ```

   Use `python -m pre_commit` instead of `pre-commit` if the command is not found. On Windows, `pip install` often puts scripts in `%APPDATA%\Python\Python314\Scripts`, which may not be on your PATH.

3. Optional — run against all tracked files:

   ```bash
   python -m pre_commit run --all-files
   ```

### Fix `pre-commit` not recognized (Windows)

Either always use the module form:

```powershell
python -m pre_commit install
python -m pre_commit run --all-files
```

Or add Python user scripts to PATH (adjust `Python314` if your version differs):

```powershell
[Environment]::SetEnvironmentVariable(
  "Path",
  $env:Path + ";$env:APPDATA\Python\Python314\Scripts",
  "User")
```

Restart the terminal after updating PATH.

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
