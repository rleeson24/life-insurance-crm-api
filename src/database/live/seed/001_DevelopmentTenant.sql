-- Dev seed: default tenant and development organization user (matches appsettings Auth:Development*).
DECLARE @TenantId uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @DevUserId uniqueidentifier = 'E1DA25DE-AF92-4E5C-A9AC-1BC186BB9A4F';
DECLARE @SystemUserId uniqueidentifier = '00000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE TenantId = @TenantId)
BEGIN
    INSERT INTO dbo.Tenants (TenantId, Name, CreatedByUserId, UpdatedByUserId)
    VALUES (@TenantId, N'Development Tenant', @SystemUserId, @SystemUserId);
END

IF NOT EXISTS (SELECT 1 FROM dbo.OrganizationUsers WHERE UserId = @DevUserId AND TenantId = @TenantId)
BEGIN
    INSERT INTO dbo.OrganizationUsers (
        TenantId, UserId, EmailAddress, DisplayName, Role, CreatedByUserId, UpdatedByUserId)
    VALUES (
        @TenantId, @DevUserId, N'dev-user@localhost', N'Development User', N'SuperAdmin', @SystemUserId, @SystemUserId);
END
ELSE IF COL_LENGTH('dbo.OrganizationUsers', 'Role') IS NOT NULL
BEGIN
    UPDATE dbo.OrganizationUsers
    SET Role = N'SuperAdmin'
    WHERE UserId = @DevUserId AND TenantId = @TenantId AND IsDeleted = 0;
END
GO
