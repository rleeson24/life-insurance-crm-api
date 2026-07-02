IF OBJECT_ID(N'dbo.MedicareEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicareEnrollments
    (
        MedicareEnrollmentId            uniqueidentifier NOT NULL CONSTRAINT PK_MedicareEnrollments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId                        uniqueidentifier NOT NULL,
        ClientId                        uniqueidentifier NOT NULL,
        RecordedAt                      datetimeoffset(7) NOT NULL,
        IsActivePlan                    bit              NOT NULL CONSTRAINT DF_MedicareEnrollments_IsActivePlan DEFAULT (0),
        PlanName                        nvarchar(200)    NULL,
        PrescriptionDrugPlan            nvarchar(200)    NULL,
        CoverageStartDate               date             NULL,
        IsNewEnrollment                 bit              NOT NULL CONSTRAINT DF_MedicareEnrollments_IsNewEnrollment DEFAULT (0),
        HealthReimbursementArrangement  nvarchar(200)    NULL,
        EnrollmentPlatform              nvarchar(200)    NULL,
        EnrollmentLocation              nvarchar(200)    NULL,
        Notes                           nvarchar(max)    NULL,
        CreatedAt                       datetimeoffset(7) NOT NULL CONSTRAINT DF_MedicareEnrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId                 uniqueidentifier NOT NULL,
        UpdatedAt                       datetimeoffset(7) NOT NULL CONSTRAINT DF_MedicareEnrollments_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId                 uniqueidentifier NOT NULL,
        IsDeleted                       bit              NOT NULL CONSTRAINT DF_MedicareEnrollments_IsDeleted DEFAULT (0),
        DeletedAt                       datetimeoffset(7) NULL,
        DeletedByUserId                 uniqueidentifier NULL,
        CONSTRAINT FK_MedicareEnrollments_Clients FOREIGN KEY (ClientId) REFERENCES dbo.Clients (ClientId),
        CONSTRAINT FK_MedicareEnrollments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO
