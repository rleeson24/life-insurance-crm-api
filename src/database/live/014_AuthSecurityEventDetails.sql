IF COL_LENGTH('dbo.AuthSecurityEvents', 'HttpStatus') IS NULL
BEGIN
    ALTER TABLE dbo.AuthSecurityEvents ADD HttpStatus smallint NULL;
END
GO

IF COL_LENGTH('dbo.AuthSecurityEvents', 'ResultCount') IS NULL
BEGIN
    ALTER TABLE dbo.AuthSecurityEvents ADD ResultCount int NULL;
END
GO

IF COL_LENGTH('dbo.AuthSecurityEvents', 'TargetId') IS NULL
BEGIN
    ALTER TABLE dbo.AuthSecurityEvents ADD TargetId uniqueidentifier NULL;
END
GO

IF COL_LENGTH('dbo.AuthSecurityEvents', 'Detail') IS NULL
BEGIN
    ALTER TABLE dbo.AuthSecurityEvents ADD Detail nvarchar(512) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AuthSecurityEvents_TargetId_OccurredAt'
      AND object_id = OBJECT_ID(N'dbo.AuthSecurityEvents'))
BEGIN
    CREATE INDEX IX_AuthSecurityEvents_TargetId_OccurredAt
        ON dbo.AuthSecurityEvents (TargetId, OccurredAt DESC)
        WHERE TargetId IS NOT NULL;
END
GO
