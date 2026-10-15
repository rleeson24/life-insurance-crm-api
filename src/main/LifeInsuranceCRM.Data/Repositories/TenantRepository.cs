using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Output;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private const string TenantSelectColumns = """
        t.TenantId, t.Name, t.IsActive,
        t.AddressLine1, t.AddressLine2, t.City, t.State, t.PostalCode,
        t.NoticesEmail, t.IncidentContact, t.IncidentTelephone,
        t.CreatedAt, t.UpdatedAt,
        (
            SELECT COUNT(*)
            FROM dbo.Clients c
            WHERE c.TenantId = t.TenantId
              AND c.IsDeleted = 0
        ) AS ClientCount
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
            FROM dbo.Tenants t
            WHERE t.IsDeleted = 0
            ORDER BY t.Name;
            """;

        using var bypass = _dbExecutor.BypassTenantFilter();
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
            FROM dbo.Tenants t
            WHERE t.TenantId = @TenantId AND t.IsDeleted = 0;
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
        TenantContactFields contact,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var tenantId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.Tenants (
                TenantId, Name, IsActive,
                AddressLine1, AddressLine2, City, State, PostalCode,
                NoticesEmail, IncidentContact, IncidentTelephone,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @TenantId, @Name, 1,
                @AddressLine1, @AddressLine2, @City, @State, @PostalCode,
                @NoticesEmail, @IncidentContact, @IncidentTelephone,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Name", name),
            TextParameter("@AddressLine1", contact.AddressLine1),
            TextParameter("@AddressLine2", contact.AddressLine2),
            TextParameter("@City", contact.City),
            TextParameter("@State", contact.State),
            TextParameter("@PostalCode", contact.PostalCode),
            TextParameter("@NoticesEmail", contact.NoticesEmail),
            TextParameter("@IncidentContact", contact.IncidentContact),
            TextParameter("@IncidentTelephone", contact.IncidentTelephone),
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
        TenantContactFields contact,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Tenants
            SET Name = COALESCE(@Name, Name),
                IsActive = COALESCE(@IsActive, IsActive),
                AddressLine1 = @AddressLine1,
                AddressLine2 = @AddressLine2,
                City = @City,
                State = @State,
                PostalCode = @PostalCode,
                NoticesEmail = @NoticesEmail,
                IncidentContact = @IncidentContact,
                IncidentTelephone = @IncidentTelephone,
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
            TextParameter("@AddressLine1", contact.AddressLine1),
            TextParameter("@AddressLine2", contact.AddressLine2),
            TextParameter("@City", contact.City),
            TextParameter("@State", contact.State),
            TextParameter("@PostalCode", contact.PostalCode),
            TextParameter("@NoticesEmail", contact.NoticesEmail),
            TextParameter("@IncidentContact", contact.IncidentContact),
            TextParameter("@IncidentTelephone", contact.IncidentTelephone),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(tenantId, cancellationToken);
    }

    private static SqlParameter TextParameter(string name, string? value) =>
        new(name, (object?)value ?? DBNull.Value);

    private static TenantDto ReadTenant(SqlDataReader reader) => new()
    {
        TenantId = reader.GetGuid("TenantId"),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        IsActive = reader.GetBoolean("IsActive"),
        AddressLine1 = reader.GetNullableString("AddressLine1"),
        AddressLine2 = reader.GetNullableString("AddressLine2"),
        City = reader.GetNullableString("City"),
        State = reader.GetNullableString("State"),
        PostalCode = reader.GetNullableString("PostalCode"),
        NoticesEmail = reader.GetNullableString("NoticesEmail"),
        IncidentContact = reader.GetNullableString("IncidentContact"),
        IncidentTelephone = reader.GetNullableString("IncidentTelephone"),
        ClientCount = reader.GetInt32("ClientCount"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
    };
}
