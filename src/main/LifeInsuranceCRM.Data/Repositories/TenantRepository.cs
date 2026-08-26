using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Output;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private const string TenantSelectColumns = """
        TenantId, Name, IsActive, CreatedAt, UpdatedAt
        """;

    private readonly IDbExecutor _dbExecutor;

    public TenantRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<TenantDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {TenantSelectColumns}
            FROM dbo.Tenants
            WHERE IsDeleted = 0
            ORDER BY Name;
            """;

        var tenants = new List<TenantDto>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    tenants.Add(ReadTenant(reader));
                }
            },
            cancellationToken);

        return tenants;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {TenantSelectColumns}
            FROM dbo.Tenants
            WHERE TenantId = @TenantId AND IsDeleted = 0;
            """;

        TenantDto? tenant = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    tenant = ReadTenant(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@TenantId", tenantId));

        return tenant;
    }

    public async Task<TenantDto> InsertAsync(
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var tenantId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.Tenants (
                TenantId, Name, IsActive, CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @TenantId, @Name, 1, @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Name", name),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(tenantId, cancellationToken))!;
    }

    public async Task<TenantDto?> UpdateAsync(
        Guid tenantId,
        string? name,
        bool? isActive,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Tenants
            SET Name = COALESCE(@Name, Name),
                IsActive = COALESCE(@IsActive, IsActive),
                UpdatedAt = @UpdatedAt,
                UpdatedByUserId = @UpdatedByUserId
            WHERE TenantId = @TenantId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Name", (object?)name ?? DBNull.Value),
            new SqlParameter("@IsActive", (object?)isActive ?? DBNull.Value),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(tenantId, cancellationToken);
    }

    private static TenantDto ReadTenant(SqlDataReader reader) => new()
    {
        TenantId = reader.GetGuid("TenantId"),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        IsActive = reader.GetBoolean("IsActive"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
    };
}
