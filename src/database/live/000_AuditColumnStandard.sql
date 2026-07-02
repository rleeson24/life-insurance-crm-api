/*
Standard audit + soft-delete columns for live dbo tables.
Include on: Tenants, Clients, ClientInteractions, MedicareEnrollments,
SupplementalEnrollments, OrganizationUsers.

All datetimeoffset columns store UTC only (offset +00:00). Use SYSUTCDATETIME()
for defaults; application code uses INowProvider / DateTimeOffset.UtcNow.
Local timezone display is a UI concern only.

    TenantId            uniqueidentifier NOT NULL,
    CreatedAt           datetimeoffset(7) NOT NULL,
    CreatedByUserId     uniqueidentifier NOT NULL,
    UpdatedAt           datetimeoffset(7) NOT NULL,
    UpdatedByUserId     uniqueidentifier NOT NULL,
    IsDeleted           bit NOT NULL CONSTRAINT DF_<Table>_IsDeleted DEFAULT (0),
    DeletedAt           datetimeoffset(7) NULL,
    DeletedByUserId     uniqueidentifier NULL,
*/
