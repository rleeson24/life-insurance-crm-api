IF OBJECT_ID(N'dbo.MedicarePlanNames', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicarePlanNames
    (
        MedicarePlanNameId  uniqueidentifier NOT NULL CONSTRAINT PK_MedicarePlanNames PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId            uniqueidentifier NOT NULL,
        PlanYear            smallint         NOT NULL,
        Name                nvarchar(200)    NOT NULL,
        CreatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_MedicarePlanNames_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId     uniqueidentifier NOT NULL,
        UpdatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_MedicarePlanNames_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId     uniqueidentifier NOT NULL,
        IsDeleted           bit              NOT NULL CONSTRAINT DF_MedicarePlanNames_IsDeleted DEFAULT (0),
        DeletedAt           datetimeoffset(7) NULL,
        DeletedByUserId     uniqueidentifier NULL,
        CONSTRAINT FK_MedicarePlanNames_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_MedicarePlanNames_Tenant_Year_Name'
      AND object_id = OBJECT_ID(N'dbo.MedicarePlanNames')
)
BEGIN
    CREATE UNIQUE INDEX UX_MedicarePlanNames_Tenant_Year_Name
        ON dbo.MedicarePlanNames (TenantId, PlanYear, Name)
        WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.DrugPlanNames', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DrugPlanNames
    (
        DrugPlanNameId      uniqueidentifier NOT NULL CONSTRAINT PK_DrugPlanNames PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId            uniqueidentifier NOT NULL,
        PlanYear            smallint         NOT NULL,
        Name                nvarchar(200)    NOT NULL,
        CreatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_DrugPlanNames_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId     uniqueidentifier NOT NULL,
        UpdatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_DrugPlanNames_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId     uniqueidentifier NOT NULL,
        IsDeleted           bit              NOT NULL CONSTRAINT DF_DrugPlanNames_IsDeleted DEFAULT (0),
        DeletedAt           datetimeoffset(7) NULL,
        DeletedByUserId     uniqueidentifier NULL,
        CONSTRAINT FK_DrugPlanNames_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_DrugPlanNames_Tenant_Year_Name'
      AND object_id = OBJECT_ID(N'dbo.DrugPlanNames')
)
BEGIN
    CREATE UNIQUE INDEX UX_DrugPlanNames_Tenant_Year_Name
        ON dbo.DrugPlanNames (TenantId, PlanYear, Name)
        WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.SecondaryPlanNames', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecondaryPlanNames
    (
        SecondaryPlanNameId uniqueidentifier NOT NULL CONSTRAINT PK_SecondaryPlanNames PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId            uniqueidentifier NOT NULL,
        PlanYear            smallint         NOT NULL,
        Name                nvarchar(200)    NOT NULL,
        CreatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_SecondaryPlanNames_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId     uniqueidentifier NOT NULL,
        UpdatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_SecondaryPlanNames_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId     uniqueidentifier NOT NULL,
        IsDeleted           bit              NOT NULL CONSTRAINT DF_SecondaryPlanNames_IsDeleted DEFAULT (0),
        DeletedAt           datetimeoffset(7) NULL,
        DeletedByUserId     uniqueidentifier NULL,
        CONSTRAINT FK_SecondaryPlanNames_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_SecondaryPlanNames_Tenant_Year_Name'
      AND object_id = OBJECT_ID(N'dbo.SecondaryPlanNames')
)
BEGIN
    CREATE UNIQUE INDEX UX_SecondaryPlanNames_Tenant_Year_Name
        ON dbo.SecondaryPlanNames (TenantId, PlanYear, Name)
        WHERE IsDeleted = 0;
END
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates sp
        INNER JOIN sys.objects o ON sp.target_object_id = o.object_id
        WHERE o.name = N'MedicarePlanNames'
          AND SCHEMA_NAME(o.schema_id) = N'dbo'
   )
BEGIN
    ALTER SECURITY POLICY dbo.TenantPolicy
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.MedicarePlanNames;
END
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates sp
        INNER JOIN sys.objects o ON sp.target_object_id = o.object_id
        WHERE o.name = N'DrugPlanNames'
          AND SCHEMA_NAME(o.schema_id) = N'dbo'
   )
BEGIN
    ALTER SECURITY POLICY dbo.TenantPolicy
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.DrugPlanNames;
END
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates sp
        INNER JOIN sys.objects o ON sp.target_object_id = o.object_id
        WHERE o.name = N'SecondaryPlanNames'
          AND SCHEMA_NAME(o.schema_id) = N'dbo'
   )
BEGIN
    ALTER SECURITY POLICY dbo.TenantPolicy
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.SecondaryPlanNames;
END
GO
