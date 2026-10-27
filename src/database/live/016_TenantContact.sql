IF COL_LENGTH('dbo.Tenants', 'AddressLine1') IS NULL
    ALTER TABLE dbo.Tenants ADD AddressLine1 nvarchar(200) NULL;
IF COL_LENGTH('dbo.Tenants', 'AddressLine2') IS NULL
    ALTER TABLE dbo.Tenants ADD AddressLine2 nvarchar(200) NULL;
IF COL_LENGTH('dbo.Tenants', 'City') IS NULL
    ALTER TABLE dbo.Tenants ADD City nvarchar(100) NULL;
IF COL_LENGTH('dbo.Tenants', 'State') IS NULL
    ALTER TABLE dbo.Tenants ADD State nvarchar(2) NULL;
IF COL_LENGTH('dbo.Tenants', 'PostalCode') IS NULL
    ALTER TABLE dbo.Tenants ADD PostalCode nvarchar(10) NULL;
IF COL_LENGTH('dbo.Tenants', 'NoticesEmail') IS NULL
    ALTER TABLE dbo.Tenants ADD NoticesEmail nvarchar(320) NULL;
IF COL_LENGTH('dbo.Tenants', 'IncidentContact') IS NULL
    ALTER TABLE dbo.Tenants ADD IncidentContact nvarchar(200) NULL;
IF COL_LENGTH('dbo.Tenants', 'IncidentTelephone') IS NULL
    ALTER TABLE dbo.Tenants ADD IncidentTelephone nvarchar(32) NULL;
GO
