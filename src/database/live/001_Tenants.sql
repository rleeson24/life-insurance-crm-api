IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        TenantId          uniqueidentifier NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name              nvarchar(200)    NOT NULL,
        IsActive          bit              NOT NULL CONSTRAINT DF_Tenants_IsActive DEFAULT (1),
        CreatedAt         datetimeoffset(7) NOT NULL CONSTRAINT DF_Tenants_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId     uniqueidentifier NOT NULL,
        UpdatedAt         datetimeoffset(7) NOT NULL CONSTRAINT DF_Tenants_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId     uniqueidentifier NOT NULL,
        IsDeleted         bit              NOT NULL CONSTRAINT DF_Tenants_IsDeleted DEFAULT (0),
        DeletedAt         datetimeoffset(7) NULL,
        DeletedByUserId   uniqueidentifier NULL
    );
END
GO
