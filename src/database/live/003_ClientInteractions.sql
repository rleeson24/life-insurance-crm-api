IF OBJECT_ID(N'dbo.ClientInteractions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientInteractions
    (
        ClientInteractionId uniqueidentifier NOT NULL CONSTRAINT PK_ClientInteractions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId            uniqueidentifier NOT NULL,
        ClientId            uniqueidentifier NOT NULL,
        ContactedAt         datetimeoffset(7) NOT NULL,
        Summary             nvarchar(500)    NULL,
        Notes               nvarchar(max)    NULL,
        RequiresFollowUp    bit              NOT NULL CONSTRAINT DF_ClientInteractions_RequiresFollowUp DEFAULT (0),
        CreatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_ClientInteractions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId     uniqueidentifier NOT NULL,
        UpdatedAt           datetimeoffset(7) NOT NULL CONSTRAINT DF_ClientInteractions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId     uniqueidentifier NOT NULL,
        IsDeleted           bit              NOT NULL CONSTRAINT DF_ClientInteractions_IsDeleted DEFAULT (0),
        DeletedAt           datetimeoffset(7) NULL,
        DeletedByUserId     uniqueidentifier NULL,
        CONSTRAINT FK_ClientInteractions_Clients FOREIGN KEY (ClientId) REFERENCES dbo.Clients (ClientId),
        CONSTRAINT FK_ClientInteractions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );

    CREATE INDEX IX_ClientInteractions_ClientId_ContactedAt ON dbo.ClientInteractions (ClientId, ContactedAt DESC);
END
GO
