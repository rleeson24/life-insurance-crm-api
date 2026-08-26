using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Output;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class OrganizationUserRepository : IOrganizationUserRepository
{
    private const string UserSelectColumns = """
        u.OrganizationUserId, u.TenantId, t.Name AS TenantName, u.UserId,
        u.EmailAddress, u.DisplayName, u.Role, u.IsActive, u.CreatedAt, u.UpdatedAt
        """;

    private readonly IDbExecutor _dbExecutor;

    public OrganizationUserRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<OrganizationUserContext?> GetUserContextAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1) TenantId, Role, IsActive
            FROM dbo.OrganizationUsers
            WHERE UserId = @UserId AND IsDeleted = 0;
            """;

        OrganizationUserContext? userContext = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    userContext = new OrganizationUserContext(
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetBoolean(2));
                }
            },
            cancellationToken,
            new SqlParameter("@UserId", userId));

        return userContext;
    }

    public async Task<IReadOnlyList<OrganizationUserDto>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {UserSelectColumns}
            FROM dbo.OrganizationUsers u
            INNER JOIN dbo.Tenants t ON t.TenantId = u.TenantId
            WHERE u.TenantId = @TenantId AND u.IsDeleted = 0
            ORDER BY u.DisplayName, u.EmailAddress;
            """;

        var users = new List<OrganizationUserDto>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    users.Add(ReadUser(reader));
                }
            },
            cancellationToken,
            new SqlParameter("@TenantId", tenantId));

        return users;
    }

    public async Task<OrganizationUserDto?> GetByOrganizationUserIdAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {UserSelectColumns}
            FROM dbo.OrganizationUsers u
            INNER JOIN dbo.Tenants t ON t.TenantId = u.TenantId
            WHERE u.OrganizationUserId = @OrganizationUserId AND u.IsDeleted = 0;
            """;

        OrganizationUserDto? user = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    user = ReadUser(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@OrganizationUserId", organizationUserId));

        return user;
    }

    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ExistsAsync(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.OrganizationUsers WHERE UserId = @UserId AND IsDeleted = 0) THEN 1 ELSE 0 END;",
            cancellationToken,
            new SqlParameter("@UserId", userId));

    public async Task<int> CountActiveAdminsInTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.OrganizationUsers
            WHERE TenantId = @TenantId
              AND Role = @Role
              AND IsActive = 1
              AND IsDeleted = 0;
            """;

        return await _dbExecutor.ExecuteScalarAsync<int>(
            sql,
            cancellationToken,
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Role", OrganizationRoles.Admin));
    }

    public Task InsertTenantAsync(
        Guid tenantId,
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Tenants (TenantId, Name, CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (@TenantId, @Name, @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        return _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Name", name),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));
    }

    public async Task<OrganizationUserDto> InsertAsync(
        Guid tenantId,
        Guid userId,
        string? emailAddress,
        string? displayName,
        string role,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var organizationUserId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.OrganizationUsers (
                OrganizationUserId, TenantId, UserId, EmailAddress, DisplayName, Role, IsActive,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @OrganizationUserId, @TenantId, @UserId, @EmailAddress, @DisplayName, @Role, 1,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@OrganizationUserId", organizationUserId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@UserId", userId),
            new SqlParameter("@EmailAddress", (object?)emailAddress ?? DBNull.Value),
            new SqlParameter("@DisplayName", (object?)displayName ?? DBNull.Value),
            new SqlParameter("@Role", role),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByOrganizationUserIdAsync(organizationUserId, cancellationToken))!;
    }

    public async Task<OrganizationUserDto?> UpdateAsync(
        Guid organizationUserId,
        string? emailAddress,
        string? displayName,
        string role,
        bool isActive,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.OrganizationUsers
            SET EmailAddress = @EmailAddress,
                DisplayName = @DisplayName,
                Role = @Role,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedByUserId = @UpdatedByUserId
            WHERE OrganizationUserId = @OrganizationUserId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@OrganizationUserId", organizationUserId),
            new SqlParameter("@EmailAddress", (object?)emailAddress ?? DBNull.Value),
            new SqlParameter("@DisplayName", (object?)displayName ?? DBNull.Value),
            new SqlParameter("@Role", role),
            new SqlParameter("@IsActive", isActive),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByOrganizationUserIdAsync(organizationUserId, cancellationToken);
    }

    private async Task<bool> ExistsAsync(
        string sql,
        CancellationToken cancellationToken,
        params SqlParameter[] parameters)
    {
        var result = await _dbExecutor.ExecuteScalarAsync<int>(sql, cancellationToken, parameters);
        return result == 1;
    }

    private static OrganizationUserDto ReadUser(SqlDataReader reader) => new()
    {
        OrganizationUserId = reader.GetGuid("OrganizationUserId"),
        TenantId = reader.GetGuid("TenantId"),
        TenantName = reader.GetNullableString("TenantName"),
        UserId = reader.GetGuid("UserId"),
        EmailAddress = reader.GetNullableString("EmailAddress"),
        DisplayName = reader.GetNullableString("DisplayName"),
        Role = reader.GetString(reader.GetOrdinal("Role")),
        IsActive = reader.GetBoolean("IsActive"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
    };
}
