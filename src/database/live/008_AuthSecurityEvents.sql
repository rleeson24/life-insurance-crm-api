IF OBJECT_ID(N'dbo.AuthSecurityEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthSecurityEvents
    (
        AuthSecurityEventId uniqueidentifier NOT NULL CONSTRAINT PK_AuthSecurityEvents PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId            uniqueidentifier NULL,
        OccurredAt          datetimeoffset(7) NOT NULL CONSTRAINT DF_AuthSecurityEvents_OccurredAt DEFAULT SYSUTCDATETIME(),
        EventType           nvarchar(64)     NOT NULL,
        UserId              uniqueidentifier NULL,
        UserEmail           nvarchar(320)    NULL,
        Success             bit              NOT NULL,
        FailureReason       nvarchar(256)    NULL,
        IpAddress           nvarchar(45)     NULL,
        UserAgent           nvarchar(512)    NULL,
        CorrelationId       nvarchar(64)     NULL,
        Resource            nvarchar(256)    NULL
    );

    CREATE INDEX IX_AuthSecurityEvents_TenantId_OccurredAt ON dbo.AuthSecurityEvents (TenantId, OccurredAt DESC);
    CREATE INDEX IX_AuthSecurityEvents_UserId_OccurredAt ON dbo.AuthSecurityEvents (UserId, OccurredAt DESC);
    CREATE INDEX IX_AuthSecurityEvents_EventType_OccurredAt ON dbo.AuthSecurityEvents (EventType, OccurredAt DESC);
END
GO
