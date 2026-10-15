IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        TenantId          uniqueidentifier NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name              nvarchar(200)    NOT NULL,
        IsActive          bit              NOT NULL CONSTRAINT DF_Tenants_IsActive DEFAULT (1),
        AddressLine1      nvarchar(200)    NULL,
        AddressLine2      nvarchar(200)    NULL,
        City              nvarchar(100)    NULL,
        State             nvarchar(2)      NULL,
        PostalCode        nvarchar(10)     NULL,
        NoticesEmail      nvarchar(320)    NULL,
        IncidentContact   nvarchar(200)    NULL,
        IncidentTelephone nvarchar(32)     NULL,
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
