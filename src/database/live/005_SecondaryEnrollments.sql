IF OBJECT_ID(N'dbo.SecondaryEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecondaryEnrollments
    (
        SecondaryEnrollmentId uniqueidentifier NOT NULL CONSTRAINT PK_SecondaryEnrollments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId              uniqueidentifier NOT NULL,
        ClientId              uniqueidentifier NOT NULL,
        RecordedAt            datetimeoffset(7) NOT NULL,
        PlanOrCarrierName     nvarchar(200)    NULL,
        CoverageStartDate     date             NULL,
        IsActiveCoverage      bit              NOT NULL CONSTRAINT DF_SecondaryEnrollments_IsActiveCoverage DEFAULT (0),
        Notes                 nvarchar(max)    NULL,
        CreatedAt             datetimeoffset(7) NOT NULL CONSTRAINT DF_SecondaryEnrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId       uniqueidentifier NOT NULL,
        UpdatedAt             datetimeoffset(7) NOT NULL CONSTRAINT DF_SecondaryEnrollments_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId       uniqueidentifier NOT NULL,
        IsDeleted             bit              NOT NULL CONSTRAINT DF_SecondaryEnrollments_IsDeleted DEFAULT (0),
        DeletedAt             datetimeoffset(7) NULL,
        DeletedByUserId       uniqueidentifier NULL,
        CONSTRAINT FK_SecondaryEnrollments_Clients FOREIGN KEY (ClientId) REFERENCES dbo.Clients (ClientId),
        CONSTRAINT FK_SecondaryEnrollments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO
