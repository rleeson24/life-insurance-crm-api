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
| **detect-secrets** | Heuristic scan against [`.secrets.baseline`](../../.secrets.baseline); fails on new findings |
| **detect-private-key** | Blocks PEM private keys |
| **check-added-large-files** | Blocks accidental commits of files larger than 500 KB |

### Bypass (emergency only)

To skip secret scanning for a single commit (requires justification and team awareness):

```bash
SKIP=gitleaks,detect-secrets git commit -m "your message"
```

Do not use bypass for real credentials. Rotate any secret that was ever committed, even if the commit was amended or reverted.

### CI backup

GitHub Actions runs the same gitleaks configuration on every push and pull request to `main`. CI catches issues if a developer commits without installing pre-commit locally.

### Dependency scanning

| Check | Where it runs | What it does |
|-------|---------------|--------------|
| **`dotnet list package --vulnerable`** | [CI](../../.github/workflows/ci.yml) `vulnerability-scan` job | Fails the PR if NuGet (direct or transitive) packages have known vulnerabilities |
| **Dependabot** | [`.github/dependabot.yml`](../../.github/dependabot.yml) | Weekly version PRs for NuGet, Docker, and GitHub Actions; security PRs as advisories are published |
| **CodeQL** (optional) | [`.github/workflows/codeql.yml`](../../.github/workflows/codeql.yml) | Static analysis for C# on push, PR, and a weekly schedule |

Private repositories need [GitHub Advanced Security](https://docs.github.com/en/get-started/learning-about-github/about-github-advanced-security) for CodeQL results to upload. Disable the workflow without deleting it by setting the repository variable `ENABLE_CODEQL` to `false`.

### Adding allowlist entries

If gitleaks reports a false positive:

1. Confirm the value is not a real secret.
2. Add the value to the `[allowlist].regexes` array in [`.gitleaks.toml`](../../.gitleaks.toml) (see the description block above it).
3. Avoid broad regexes that could hide actual leaks.

Development GUIDs and Azure RBAC role definition IDs are already allowlisted.

If **detect-secrets** reports a false positive, confirm it is not a real secret, then regenerate the baseline:

```bash
python -m detect_secrets scan --baseline .secrets.baseline
```

Review the diff in `.secrets.baseline` before committing it.
