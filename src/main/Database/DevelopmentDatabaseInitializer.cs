using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.API.Database;

public interface IDevelopmentDatabaseInitializer
{
    Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default);
}

public sealed class DevelopmentDatabaseInitializer : IDevelopmentDatabaseInitializer
{
    private const string AddRoleColumnSql = """
        IF COL_LENGTH('dbo.OrganizationUsers', 'Role') IS NULL
        BEGIN
            ALTER TABLE dbo.OrganizationUsers
                ADD Role nvarchar(50) NOT NULL CONSTRAINT DF_OrganizationUsers_Role DEFAULT 'Agent';
        END
        """;

    private const string EnsureDevUserSql = """
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
                TenantId, UserId, EmailAddress, DisplayName, Role, CreatedByUserId, UpdatedByUserId)
            VALUES (
                @TenantId, @DevUserId, N'dev-user@localhost', N'Development User', N'Admin', @SystemUserId, @SystemUserId);
        END
        ELSE
        BEGIN
            UPDATE dbo.OrganizationUsers
            SET Role = N'Admin'
            WHERE UserId = @DevUserId AND TenantId = @TenantId AND IsDeleted = 0;
        END
        """;

    public async Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(connection, AddRoleColumnSql, cancellationToken);
        await ExecuteNonQueryAsync(connection, EnsureDevUserSql, cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
