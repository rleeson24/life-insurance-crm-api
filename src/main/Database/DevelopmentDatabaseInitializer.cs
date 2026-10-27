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
                WHERE sp.object_id = OBJECT_ID(N'dbo.TenantPolicy')
                  AND sp.target_object_id = OBJECT_ID(N'dbo.ImportMappingProfiles')
                  AND sp.predicate_type = 0
           )
        BEGIN
            -- Compiled immediately, so a direct ALTER fails while the predicate exists
            -- even inside this IF. Defer it until the check has passed.
            EXEC(N'ALTER SECURITY POLICY dbo.TenantPolicy ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.ImportMappingProfiles;');
        END
        """;

    private const string EnsureTenantContactSql = """
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
            await ExecuteNonQueryAsync(connection, EnsureTenantContactSql, cancellationToken);
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
