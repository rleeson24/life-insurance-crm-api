IF COL_LENGTH('dbo.OrganizationUsers', 'Role') IS NULL
BEGIN
    ALTER TABLE dbo.OrganizationUsers
        ADD Role nvarchar(50) NOT NULL CONSTRAINT DF_OrganizationUsers_Role DEFAULT 'Agent';
END
GO
