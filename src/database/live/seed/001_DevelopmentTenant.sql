-- Dev seed: default tenant and development organization user (matches appsettings Auth:Development*).
DECLARE @TenantId uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @DevUserId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @SystemUserId uniqueidentifier = '00000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE TenantId = @TenantId)
BEGIN
    INSERT INTO dbo.Tenants (TenantId, Name, CreatedByUserId, UpdatedByUserId)
    VALUES (@TenantId, N'Development Tenant', @SystemUserId, @SystemUserId);
END

IF NOT EXISTS (SELECT 1 FROM dbo.OrganizationUsers WHERE UserId = @DevUserId AND TenantId = @TenantId)
BEGIN
    INSERT INTO dbo.OrganizationUsers (
        TenantId, UserId, EmailAddress, DisplayName, CreatedByUserId, UpdatedByUserId)
    VALUES (
        @TenantId, @DevUserId, N'dev-user@localhost', N'Development User', @SystemUserId, @SystemUserId);
END
GO
