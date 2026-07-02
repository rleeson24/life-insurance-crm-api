IF OBJECT_ID(N'dbo.SupplementalEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplementalEnrollments
    (
        SupplementalEnrollmentId uniqueidentifier NOT NULL CONSTRAINT PK_SupplementalEnrollments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId                 uniqueidentifier NOT NULL,
        ClientId                 uniqueidentifier NOT NULL,
        RecordedAt               datetimeoffset(7) NOT NULL,
        PlanOrCarrierName        nvarchar(200)    NULL,
        CoverageStartDate        date             NULL,
        IsActiveCoverage         bit              NOT NULL CONSTRAINT DF_SupplementalEnrollments_IsActiveCoverage DEFAULT (0),
        Notes                    nvarchar(max)    NULL,
        CreatedAt                datetimeoffset(7) NOT NULL CONSTRAINT DF_SupplementalEnrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId          uniqueidentifier NOT NULL,
        UpdatedAt                datetimeoffset(7) NOT NULL CONSTRAINT DF_SupplementalEnrollments_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId          uniqueidentifier NOT NULL,
        IsDeleted                bit              NOT NULL CONSTRAINT DF_SupplementalEnrollments_IsDeleted DEFAULT (0),
        DeletedAt                datetimeoffset(7) NULL,
        DeletedByUserId          uniqueidentifier NULL,
        CONSTRAINT FK_SupplementalEnrollments_Clients FOREIGN KEY (ClientId) REFERENCES dbo.Clients (ClientId),
        CONSTRAINT FK_SupplementalEnrollments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO
