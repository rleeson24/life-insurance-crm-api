# BrokerBook Database Scripts

## Layout

- `live/` — application schema (RLS, audit columns, domain-first naming)
- `migrate/` — Access-shaped staging + Phase 2 map scripts

## Applying live scripts

Canonical runner: [`apply-live-schema.ps1`](apply-live-schema.ps1). It applies `001`–`011` in the same order as Aspire `LiveSchemaScripts`. Scripts are idempotent.

**Azure SQL** (private-endpoint server; uses your Entra login and briefly opens public access):

```powershell
cd src/database
.\apply-live-schema.ps1
```

Do not pass `-IncludeSeed` on Azure. Map yourself afterward with `scripts/provision-organization-user.ps1`.

**Local SQL** (Windows auth):

```powershell
.\apply-live-schema.ps1 -Server "localhost,1433" -UseIntegratedSecurity -IncludeSeed
```

`apply-live-schema.cmd` is a wrapper for that local path.

**Aspire:** Start the Aspire AppHost (not the API project alone). On first database creation, AppHost runs the same live scripts via `WithCreationScript` (including the dev seed). The volume is persistent, so later files such as `011` are not replayed — run `apply-live-schema.ps1` against the local container if the schema is behind.

Standalone API: set `Database:ConnectionString` in `appsettings.Development.json`, or apply with the script above.

## Constants

| Name | GUID | Purpose |
|------|------|---------|
| `MigrationSystemUserId` | `00000000-0000-0000-0000-000000000001` | Audit columns during Phase 2 migration |
| Development tenant | `22222222-2222-2222-2222-222222222222` | Local Aspire / dev auth |
| Development user | `11111111-1111-1111-1111-111111111111` | Matches `Auth:DevelopmentUserId` |

## RLS

API sets `SESSION_CONTEXT('TenantId')` after JWT validation. `OrganizationUsers` is **not** RLS-protected so tenant resolution can query by `UserId` before session context is established.

## Medicare dates

Part A/B effective dates live on `Clients`. Plan coverage start dates live on `MajorMedicalEnrollments.CoverageStartDate` and `DrugPlanEnrollments.CoverageStartDate` — do not derive one from the other.

## Date and time

| Kind | SQL type | C# type | Notes |
|------|----------|---------|-------|
| Calendar date (DOB, coverage start) | `date` | `DateOnly` | No time component |
| Instant (audit, events, interactions) | `datetimeoffset(7)` | `DateTimeOffset` | **UTC only** — always offset `+00:00` |

- Defaults and writes: `SYSUTCDATETIME()` in SQL; `INowProvider.UtcNow` or `DateTimeOffset.UtcNow` in C#.
- Never persist server-local or user-local offsets; the React UI converts UTC to local time for display.
- API responses serialize instants as ISO 8601 with `Z` (UTC).
