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

    private const string EnsureImportMappingProfilesSql = """
        IF OBJECT_ID(N'dbo.ImportMappingProfiles', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportMappingProfiles
            (
                ImportMappingProfileId uniqueidentifier NOT NULL CONSTRAINT PK_ImportMappingProfiles PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                TenantId               uniqueidentifier NOT NULL,
                Name                   nvarchar(100)    NOT NULL,
                Definition             nvarchar(max)    NOT NULL,
                CreatedAt              datetimeoffset(7) NOT NULL CONSTRAINT DF_ImportMappingProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
                CreatedByUserId        uniqueidentifier NOT NULL,
                UpdatedAt              datetimeoffset(7) NOT NULL CONSTRAINT DF_ImportMappingProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
                UpdatedByUserId        uniqueidentifier NOT NULL,
                IsDeleted              bit              NOT NULL CONSTRAINT DF_ImportMappingProfiles_IsDeleted DEFAULT (0),
                DeletedAt              datetimeoffset(7) NULL,
                DeletedByUserId        uniqueidentifier NULL,
                CONSTRAINT FK_ImportMappingProfiles_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (TenantId)
            );
        END
        """;

    private const string EnsureImportMappingProfileIndexSql = """
        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'UX_ImportMappingProfiles_Tenant_Name'
              AND object_id = OBJECT_ID(N'dbo.ImportMappingProfiles')
        )
        BEGIN
            CREATE UNIQUE INDEX UX_ImportMappingProfiles_Tenant_Name
                ON dbo.ImportMappingProfiles (TenantId, Name)
                WHERE IsDeleted = 0;
        END
        """;

    private const string EnsureImportMappingProfilePolicySql = """
        IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
           AND OBJECT_ID(N'dbo.ImportMappingProfiles', N'U') IS NOT NULL
           AND NOT EXISTS (
                SELECT 1
                FROM sys.security_predicates sp
                INNER JOIN sys.objects o ON sp.target_object_id = o.object_id
                WHERE o.name = N'ImportMappingProfiles'
                  AND SCHEMA_NAME(o.schema_id) = N'dbo'
           )
        BEGIN
            ALTER SECURITY POLICY dbo.TenantPolicy
                ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.ImportMappingProfiles;
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

    private readonly ILogger<DevelopmentDatabaseInitializer> _logger;

    public DevelopmentDatabaseInitializer(ILogger<DevelopmentDatabaseInitializer> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing development database");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await ExecuteNonQueryAsync(connection, AddRoleColumnSql, cancellationToken);
            await ExecuteNonQueryAsync(connection, EnsureImportMappingProfilesSql, cancellationToken);
            await ExecuteNonQueryAsync(connection, EnsureImportMappingProfileIndexSql, cancellationToken);
            await ExecuteNonQueryAsync(connection, EnsureImportMappingProfilePolicySql, cancellationToken);
            await ExecuteNonQueryAsync(connection, EnsureDevUserSql, cancellationToken);

            _logger.LogInformation("Development database initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Development database initialization failed");
            throw;
        }
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
