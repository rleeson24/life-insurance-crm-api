IF OBJECT_ID(N'dbo.OrganizationUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrganizationUsers
    (
        OrganizationUserId uniqueidentifier NOT NULL CONSTRAINT PK_OrganizationUsers PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId           uniqueidentifier NOT NULL,
        UserId             uniqueidentifier NOT NULL,
        EmailAddress       nvarchar(320)    NULL,
        DisplayName        nvarchar(200)    NULL,
        IsActive           bit              NOT NULL CONSTRAINT DF_OrganizationUsers_IsActive DEFAULT (1),
        CreatedAt          datetimeoffset(7) NOT NULL CONSTRAINT DF_OrganizationUsers_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    uniqueidentifier NOT NULL,
        UpdatedAt          datetimeoffset(7) NOT NULL CONSTRAINT DF_OrganizationUsers_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId    uniqueidentifier NOT NULL,
        IsDeleted          bit              NOT NULL CONSTRAINT DF_OrganizationUsers_IsDeleted DEFAULT (0),
        DeletedAt          datetimeoffset(7) NULL,
        DeletedByUserId    uniqueidentifier NULL,
        CONSTRAINT FK_OrganizationUsers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId),
        CONSTRAINT UQ_OrganizationUsers_Tenant_User UNIQUE (TenantId, UserId)
    );

    CREATE INDEX IX_OrganizationUsers_UserId ON dbo.OrganizationUsers (UserId) WHERE IsDeleted = 0;
END
GO
