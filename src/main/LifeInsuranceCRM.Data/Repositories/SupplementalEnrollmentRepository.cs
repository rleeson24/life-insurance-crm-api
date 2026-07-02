using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class SupplementalEnrollmentRepository : ISupplementalEnrollmentRepository
{
    private readonly IDbExecutor _dbExecutor;

    public SupplementalEnrollmentRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<SupplementalEnrollment>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT SupplementalEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                   CoverageStartDate, IsActiveCoverage, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.SupplementalEnrollments
            WHERE ClientId = @ClientId AND IsDeleted = 0
            ORDER BY RecordedAt DESC;
            """;

        var items = new List<SupplementalEnrollment>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadEnrollment(reader));
                }
            },
            cancellationToken,
            new SqlParameter("@ClientId", clientId));

        return items;
    }

    public async Task<SupplementalEnrollment?> GetByIdAsync(
        Guid clientId,
        Guid supplementalEnrollmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT SupplementalEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                   CoverageStartDate, IsActiveCoverage, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.SupplementalEnrollments
            WHERE ClientId = @ClientId AND SupplementalEnrollmentId = @SupplementalEnrollmentId AND IsDeleted = 0;
            """;

        SupplementalEnrollment? enrollment = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    enrollment = ReadEnrollment(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@SupplementalEnrollmentId", supplementalEnrollmentId));

        return enrollment;
    }

    public async Task<SupplementalEnrollment> InsertAsync(
        CreateSupplementalEnrollmentModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var supplementalEnrollmentId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.SupplementalEnrollments (
                SupplementalEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                CoverageStartDate, IsActiveCoverage, Notes,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @SupplementalEnrollmentId, @TenantId, @ClientId, @RecordedAt, @PlanOrCarrierName,
                @CoverageStartDate, @IsActiveCoverage, @Notes,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@SupplementalEnrollmentId", supplementalEnrollmentId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@PlanOrCarrierName", (object?)model.PlanOrCarrierName ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsActiveCoverage", model.IsActiveCoverage),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(model.ClientId, supplementalEnrollmentId, cancellationToken))!;
    }

    public async Task<SupplementalEnrollment?> UpdateAsync(
        UpdateSupplementalEnrollmentModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.SupplementalEnrollments SET
                RecordedAt = @RecordedAt, PlanOrCarrierName = @PlanOrCarrierName,
                CoverageStartDate = @CoverageStartDate, IsActiveCoverage = @IsActiveCoverage, Notes = @Notes,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND SupplementalEnrollmentId = @SupplementalEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@SupplementalEnrollmentId", model.SupplementalEnrollmentId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@PlanOrCarrierName", (object?)model.PlanOrCarrierName ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsActiveCoverage", model.IsActiveCoverage),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(model.ClientId, model.SupplementalEnrollmentId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid clientId,
        Guid supplementalEnrollmentId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.SupplementalEnrollments SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND SupplementalEnrollmentId = @SupplementalEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@SupplementalEnrollmentId", supplementalEnrollmentId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static SupplementalEnrollment ReadEnrollment(SqlDataReader reader) => new()
    {
        SupplementalEnrollmentId = reader.GetGuid("SupplementalEnrollmentId"),
        TenantId = reader.GetGuid("TenantId"),
        ClientId = reader.GetGuid("ClientId"),
        RecordedAt = reader.GetDateTimeOffset("RecordedAt"),
        PlanOrCarrierName = reader.GetNullableString("PlanOrCarrierName"),
        CoverageStartDate = reader.GetNullableDateOnly("CoverageStartDate"),
        IsActiveCoverage = reader.GetBoolean("IsActiveCoverage"),
        Notes = reader.GetNullableString("Notes"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };
}
