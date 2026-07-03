# LifeInsuranceCRM Database Scripts

## Layout

- `live/` — application schema (RLS, audit columns, domain-first naming)
- `migrate/` — Access-shaped staging + Phase 2 map scripts

## Applying live scripts (local)

Run in order against the target database:

1. `live/001_Tenants.sql` through `live/008_RLS.sql`
2. Optional dev seed: `live/seed/001_DevelopmentTenant.sql`

**Aspire:** Start `LifeInsuranceCRM.AppHost` (not the API project alone). Aspire injects `ConnectionStrings:LifeInsuranceCRM` pointing at the SQL container; the API prefers that over `Database:ConnectionString`. On first database creation, AppHost runs the live schema scripts automatically via `WithCreationScript` (same order as below, including dev seed).

**Standalone API:** Run SQL locally on port 1433 and set `Database:ConnectionString` in `appsettings.Development.json`, or apply scripts against your instance with `apply-live-schema.cmd`.

## Constants

| Name | GUID | Purpose |
|------|------|---------|
| `MigrationSystemUserId` | `00000000-0000-0000-0000-000000000001` | Audit columns during Phase 2 migration |
| Development tenant | `22222222-2222-2222-2222-222222222222` | Local Aspire / dev auth |
| Development user | `11111111-1111-1111-1111-111111111111` | Matches `Auth:DevelopmentUserId` |

## RLS

API sets `SESSION_CONTEXT('TenantId')` after JWT validation. `OrganizationUsers` is **not** RLS-protected so tenant resolution can query by `UserId` before session context is established.

## Medicare dates

Part A/B effective dates live on `Clients`. Plan coverage start dates live on `MedicareEnrollments.CoverageStartDate` — do not derive one from the other.

## Date and time

| Kind | SQL type | C# type | Notes |
|------|----------|---------|-------|
| Calendar date (DOB, coverage start) | `date` | `DateOnly` | No time component |
| Instant (audit, events, interactions) | `datetimeoffset(7)` | `DateTimeOffset` | **UTC only** — always offset `+00:00` |

- Defaults and writes: `SYSUTCDATETIME()` in SQL; `INowProvider.UtcNow` or `DateTimeOffset.UtcNow` in C#.
- Never persist server-local or user-local offsets; the React UI converts UTC to local time for display.
- API responses serialize instants as ISO 8601 with `Z` (UTC).
