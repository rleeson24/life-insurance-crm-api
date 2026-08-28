IF OBJECT_ID(N'dbo.DrugPlanEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DrugPlanEnrollments
    (
        DrugPlanEnrollmentId            uniqueidentifier NOT NULL CONSTRAINT PK_DrugPlanEnrollments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId                        uniqueidentifier NOT NULL,
        ClientId                        uniqueidentifier NOT NULL,
        RecordedAt                      datetimeoffset(7) NOT NULL,
        IsActivePlan                    bit              NOT NULL CONSTRAINT DF_DrugPlanEnrollments_IsActivePlan DEFAULT (0),
        PlanName                        nvarchar(200)    NULL,
        CoverageStartDate               date             NULL,
        IsNewEnrollment                 bit              NOT NULL CONSTRAINT DF_DrugPlanEnrollments_IsNewEnrollment DEFAULT (0),
        HealthReimbursementArrangement  bit              NOT NULL CONSTRAINT DF_DrugPlanEnrollments_HealthReimbursementArrangement DEFAULT (0),
        EnrollmentPlatform              nvarchar(200)    NULL,
        EnrollmentLocation              nvarchar(200)    NULL,
        Notes                           nvarchar(max)    NULL,
        CreatedAt                       datetimeoffset(7) NOT NULL CONSTRAINT DF_DrugPlanEnrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId                 uniqueidentifier NOT NULL,
        UpdatedAt                       datetimeoffset(7) NOT NULL CONSTRAINT DF_DrugPlanEnrollments_UpdatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId                 uniqueidentifier NOT NULL,
        IsDeleted                       bit              NOT NULL CONSTRAINT DF_DrugPlanEnrollments_IsDeleted DEFAULT (0),
        DeletedAt                       datetimeoffset(7) NULL,
        DeletedByUserId                 uniqueidentifier NULL,
        CONSTRAINT FK_DrugPlanEnrollments_Clients FOREIGN KEY (ClientId) REFERENCES dbo.Clients (ClientId),
        CONSTRAINT FK_DrugPlanEnrollments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
    );
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.DrugPlanEnrollments')
      AND c.name = N'HealthReimbursementArrangement'
      AND t.name = N'nvarchar'
)
BEGIN
    ALTER TABLE dbo.DrugPlanEnrollments DROP COLUMN HealthReimbursementArrangement;
END
GO

IF COL_LENGTH(N'dbo.DrugPlanEnrollments', N'HealthReimbursementArrangement') IS NULL
BEGIN
    ALTER TABLE dbo.DrugPlanEnrollments
        ADD HealthReimbursementArrangement bit NOT NULL
            CONSTRAINT DF_DrugPlanEnrollments_HealthReimbursementArrangement DEFAULT (0);
END
GO
