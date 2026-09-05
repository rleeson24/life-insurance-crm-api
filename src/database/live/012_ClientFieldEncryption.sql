-- Application-layer AES-GCM for client PHI fields and Medicare number blind index.
-- Existing plaintext cannot be converted in T-SQL without the DEK; values are discarded on type change.
-- Fresh databases from 002_Clients.sql already use the target shapes; this script is mostly no-op there.

IF COL_LENGTH(N'dbo.Clients', N'MedicareNumber') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'MedicareNumberEncrypted') IS NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'MedicareNumber'
          AND TYPE_NAME(system_type_id) IN (N'nvarchar', N'varchar', N'nchar', N'char'))
BEGIN
    ALTER TABLE dbo.Clients ADD MedicareNumberEncrypted varbinary(max) NULL;
END
GO

IF COL_LENGTH(N'dbo.Clients', N'MedicareNumberEncrypted') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'MedicareNumber') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'MedicareNumber'
          AND TYPE_NAME(system_type_id) IN (N'nvarchar', N'varchar', N'nchar', N'char'))
BEGIN
    ALTER TABLE dbo.Clients DROP COLUMN MedicareNumber;
    EXEC sp_rename N'dbo.Clients.MedicareNumberEncrypted', N'MedicareNumber', N'COLUMN';
END
GO

IF COL_LENGTH(N'dbo.Clients', N'DateOfBirth') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'DateOfBirthEncrypted') IS NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'DateOfBirth'
          AND TYPE_NAME(system_type_id) = N'date')
BEGIN
    ALTER TABLE dbo.Clients ADD DateOfBirthEncrypted varbinary(max) NULL;
END
GO

IF COL_LENGTH(N'dbo.Clients', N'DateOfBirthEncrypted') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'DateOfBirth') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'DateOfBirth'
          AND TYPE_NAME(system_type_id) = N'date')
BEGIN
    ALTER TABLE dbo.Clients DROP COLUMN DateOfBirth;
    EXEC sp_rename N'dbo.Clients.DateOfBirthEncrypted', N'DateOfBirth', N'COLUMN';
END
GO

IF COL_LENGTH(N'dbo.Clients', N'MedicarePartAEffectiveDate') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'MedicarePartAEffectiveDateEncrypted') IS NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'MedicarePartAEffectiveDate'
          AND TYPE_NAME(system_type_id) = N'date')
BEGIN
    ALTER TABLE dbo.Clients ADD MedicarePartAEffectiveDateEncrypted varbinary(max) NULL;
END
GO

IF COL_LENGTH(N'dbo.Clients', N'MedicarePartAEffectiveDateEncrypted') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'MedicarePartAEffectiveDate') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'MedicarePartAEffectiveDate'
          AND TYPE_NAME(system_type_id) = N'date')
BEGIN
    ALTER TABLE dbo.Clients DROP COLUMN MedicarePartAEffectiveDate;
    EXEC sp_rename N'dbo.Clients.MedicarePartAEffectiveDateEncrypted', N'MedicarePartAEffectiveDate', N'COLUMN';
END
GO

IF COL_LENGTH(N'dbo.Clients', N'MedicarePartBEffectiveDate') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'MedicarePartBEffectiveDateEncrypted') IS NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'MedicarePartBEffectiveDate'
          AND TYPE_NAME(system_type_id) = N'date')
BEGIN
    ALTER TABLE dbo.Clients ADD MedicarePartBEffectiveDateEncrypted varbinary(max) NULL;
END
GO

IF COL_LENGTH(N'dbo.Clients', N'MedicarePartBEffectiveDateEncrypted') IS NOT NULL
   AND COL_LENGTH(N'dbo.Clients', N'MedicarePartBEffectiveDate') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Clients')
          AND name = N'MedicarePartBEffectiveDate'
          AND TYPE_NAME(system_type_id) = N'date')
BEGIN
    ALTER TABLE dbo.Clients DROP COLUMN MedicarePartBEffectiveDate;
    EXEC sp_rename N'dbo.Clients.MedicarePartBEffectiveDateEncrypted', N'MedicarePartBEffectiveDate', N'COLUMN';
END
GO

IF COL_LENGTH(N'dbo.Clients', N'MedicareNumberBlindIndex') IS NULL
BEGIN
    ALTER TABLE dbo.Clients ADD MedicareNumberBlindIndex varbinary(32) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Clients_TenantId_MedicareNumberBlindIndex'
      AND object_id = OBJECT_ID(N'dbo.Clients'))
BEGIN
    CREATE UNIQUE INDEX UX_Clients_TenantId_MedicareNumberBlindIndex
        ON dbo.Clients (TenantId, MedicareNumberBlindIndex)
        WHERE MedicareNumberBlindIndex IS NOT NULL AND IsDeleted = 0;
END
GO
