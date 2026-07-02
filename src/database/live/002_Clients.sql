IF OBJECT_ID(N'dbo.Clients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clients
    (
        ClientId                      uniqueidentifier NOT NULL CONSTRAINT PK_Clients PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId                      uniqueidentifier NOT NULL,
        FirstName                     nvarchar(100)    NULL,
        LastName                      nvarchar(100)    NULL,
        LegalName                     nvarchar(200)    NULL,
        HouseholdName                 nvarchar(200)    NULL,
        PrimaryPhone                  nvarchar(32)     NULL,
        AddressLine1                  nvarchar(200)    NULL,
        AddressLine2                  nvarchar(200)    NULL,
        City                          nvarchar(100)    NULL,
        State                         nvarchar(2)      NULL,
        PostalCode                    nvarchar(10)     NULL,
        EmailAddress                  nvarchar(320)    NULL,
        DateOfBirth                   date             NULL,
        MedicareNumber                nvarchar(32)     NULL,
        MedicarePartAEffectiveDate    date             NULL,
        MedicarePartBEffectiveDate    date             NULL,
        IsActive                      bit              NOT NULL CONSTRAINT DF_Clients_IsActive DEFAULT (1),
        IsAcaClient                   bit              NOT NULL CONSTRAINT DF_Clients_IsAcaClient DEFAULT (0),
        HasContactConsent             bit              NOT NULL CONSTRAINT DF_Clients_HasContactConsent DEFAULT (0),
        Notes                         nvarchar(max)    NULL,
        CreatedAt                     datetimeoffset(7) NOT NULL CONSTRAINT DF_Clients_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId               uniqueidentifier NOT NULL,
        UpdatedAt                     datetimeoffset(7) NOT NULL CONSTRAINT DF_Clients_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId               uniqueidentifier NOT NULL,
        IsDeleted                     bit              NOT NULL CONSTRAINT DF_Clients_IsDeleted DEFAULT (0),
        DeletedAt                     datetimeoffset(7) NULL,
        DeletedByUserId               uniqueidentifier NULL,
        CONSTRAINT FK_Clients_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );

    CREATE INDEX IX_Clients_TenantId_UpdatedAt ON dbo.Clients (TenantId, UpdatedAt DESC);
END
GO
