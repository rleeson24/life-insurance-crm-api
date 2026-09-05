using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class SecondaryEnrollmentRepository : ISecondaryEnrollmentRepository
{
    private readonly IDbExecutor _dbExecutor;

    public SecondaryEnrollmentRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<SecondaryEnrollment>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT SecondaryEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                   CoverageStartDate, IsActiveCoverage, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.SecondaryEnrollments
            WHERE ClientId = @ClientId AND IsDeleted = 0
            ORDER BY RecordedAt DESC;
            """;

        var items = new List<SecondaryEnrollment>();
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

    public async Task<SecondaryEnrollment?> GetByIdAsync(
        Guid clientId,
        Guid secondaryEnrollmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT SecondaryEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                   CoverageStartDate, IsActiveCoverage, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.SecondaryEnrollments
            WHERE ClientId = @ClientId AND SecondaryEnrollmentId = @SecondaryEnrollmentId AND IsDeleted = 0;
            """;

        SecondaryEnrollment? enrollment = null;
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
            new SqlParameter("@SecondaryEnrollmentId", secondaryEnrollmentId));

        return enrollment;
    }

    public async Task<SecondaryEnrollment> InsertAsync(
        CreateSecondaryEnrollmentModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var secondaryEnrollmentId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.SecondaryEnrollments (
                SecondaryEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                CoverageStartDate, IsActiveCoverage, Notes,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @SecondaryEnrollmentId, @TenantId, @ClientId, @RecordedAt, @PlanOrCarrierName,
                @CoverageStartDate, @IsActiveCoverage, @Notes,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@SecondaryEnrollmentId", secondaryEnrollmentId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@PlanOrCarrierName", (object?)model.PlanOrCarrierName ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsActiveCoverage", true),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(model.ClientId, secondaryEnrollmentId, cancellationToken))!;
    }

    public async Task<SecondaryEnrollment?> UpdateAsync(
        UpdateSecondaryEnrollmentModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.SecondaryEnrollments SET
                RecordedAt = @RecordedAt, PlanOrCarrierName = @PlanOrCarrierName,
                CoverageStartDate = @CoverageStartDate, IsActiveCoverage = @IsActiveCoverage, Notes = @Notes,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND SecondaryEnrollmentId = @SecondaryEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@SecondaryEnrollmentId", model.SecondaryEnrollmentId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@PlanOrCarrierName", (object?)model.PlanOrCarrierName ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsActiveCoverage", model.IsActiveCoverage),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(model.ClientId, model.SecondaryEnrollmentId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid clientId,
        Guid secondaryEnrollmentId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.SecondaryEnrollments SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND SecondaryEnrollmentId = @SecondaryEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@SecondaryEnrollmentId", secondaryEnrollmentId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static SecondaryEnrollment ReadEnrollment(SqlDataReader reader) => new()
    {
        SecondaryEnrollmentId = reader.GetGuid("SecondaryEnrollmentId"),
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
