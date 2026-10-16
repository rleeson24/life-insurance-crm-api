-- Saved import layouts. The definition is column names and sheet roles only.
-- Client rows never belong in this table.

IF OBJECT_ID(N'dbo.ImportMappingProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportMappingProfiles
    (
        ImportMappingProfileId uniqueidentifier NOT NULL CONSTRAINT PK_ImportMappingProfiles PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId               uniqueidentifier NOT NULL,
        Name                   nvarchar(100)    NOT NULL,
        Definition             nvarchar(max)    NOT NULL,
        CreatedAt              datetimeoffset(7) NOT NULL CONSTRAINT DF_ImportMappingProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId        uniqueidentifier NOT NULL,
        UpdatedAt              datetimeoffset(7) NOT NULL CONSTRAINT DF_ImportMappingProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId        uniqueidentifier NOT NULL,
        IsDeleted              bit              NOT NULL CONSTRAINT DF_ImportMappingProfiles_IsDeleted DEFAULT (0),
        DeletedAt              datetimeoffset(7) NULL,
        DeletedByUserId        uniqueidentifier NULL,
        CONSTRAINT FK_ImportMappingProfiles_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_ImportMappingProfiles_Tenant_Name'
      AND object_id = OBJECT_ID(N'dbo.ImportMappingProfiles')
)
BEGIN
    CREATE UNIQUE INDEX UX_ImportMappingProfiles_Tenant_Name
        ON dbo.ImportMappingProfiles (TenantId, Name)
        WHERE IsDeleted = 0;
END
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates sp
        INNER JOIN sys.objects o ON sp.target_object_id = o.object_id
        WHERE o.name = N'ImportMappingProfiles'
          AND SCHEMA_NAME(o.schema_id) = N'dbo'
   )
BEGIN
    ALTER SECURITY POLICY dbo.TenantPolicy
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.ImportMappingProfiles;
END
GO
