using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class MajorMedicalEnrollmentRepository : IMajorMedicalEnrollmentRepository
{
    private readonly IDbExecutor _dbExecutor;

    public MajorMedicalEnrollmentRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<MajorMedicalEnrollment>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT MajorMedicalEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                   CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                   EnrollmentPlatform, EnrollmentLocation, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.MajorMedicalEnrollments
            WHERE ClientId = @ClientId AND IsDeleted = 0
            ORDER BY RecordedAt DESC;
            """;

        var items = new List<MajorMedicalEnrollment>();
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

    public async Task<MajorMedicalEnrollment?> GetByIdAsync(
        Guid clientId,
        Guid majorMedicalEnrollmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT MajorMedicalEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                   CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                   EnrollmentPlatform, EnrollmentLocation, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.MajorMedicalEnrollments
            WHERE ClientId = @ClientId AND MajorMedicalEnrollmentId = @MajorMedicalEnrollmentId AND IsDeleted = 0;
            """;

        MajorMedicalEnrollment? enrollment = null;
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
            new SqlParameter("@MajorMedicalEnrollmentId", majorMedicalEnrollmentId));

        return enrollment;
    }

    public async Task<MajorMedicalEnrollment> InsertAsync(
        CreateMajorMedicalEnrollmentModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var majorMedicalEnrollmentId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.MajorMedicalEnrollments (
                MajorMedicalEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                EnrollmentPlatform, EnrollmentLocation, Notes,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @MajorMedicalEnrollmentId, @TenantId, @ClientId, @RecordedAt, @IsActivePlan, @PlanName,
                @CoverageStartDate, @IsNewEnrollment, @HealthReimbursementArrangement,
                @EnrollmentPlatform, @EnrollmentLocation, @Notes,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@MajorMedicalEnrollmentId", majorMedicalEnrollmentId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@IsActivePlan", model.IsActivePlan),
            new SqlParameter("@PlanName", (object?)model.PlanName ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsNewEnrollment", model.IsNewEnrollment),
            new SqlParameter("@HealthReimbursementArrangement", model.HealthReimbursementArrangement),
            new SqlParameter("@EnrollmentPlatform", (object?)model.EnrollmentPlatform ?? DBNull.Value),
            new SqlParameter("@EnrollmentLocation", (object?)model.EnrollmentLocation ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(model.ClientId, majorMedicalEnrollmentId, cancellationToken))!;
    }

    public async Task<MajorMedicalEnrollment?> UpdateAsync(
        UpdateMajorMedicalEnrollmentModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.MajorMedicalEnrollments SET
                RecordedAt = @RecordedAt, IsActivePlan = @IsActivePlan, PlanName = @PlanName,
                CoverageStartDate = @CoverageStartDate,
                IsNewEnrollment = @IsNewEnrollment, HealthReimbursementArrangement = @HealthReimbursementArrangement,
                EnrollmentPlatform = @EnrollmentPlatform, EnrollmentLocation = @EnrollmentLocation, Notes = @Notes,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND MajorMedicalEnrollmentId = @MajorMedicalEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@MajorMedicalEnrollmentId", model.MajorMedicalEnrollmentId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@IsActivePlan", model.IsActivePlan),
            new SqlParameter("@PlanName", (object?)model.PlanName ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsNewEnrollment", model.IsNewEnrollment),
            new SqlParameter("@HealthReimbursementArrangement", model.HealthReimbursementArrangement),
            new SqlParameter("@EnrollmentPlatform", (object?)model.EnrollmentPlatform ?? DBNull.Value),
            new SqlParameter("@EnrollmentLocation", (object?)model.EnrollmentLocation ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(model.ClientId, model.MajorMedicalEnrollmentId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid clientId,
        Guid majorMedicalEnrollmentId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.MajorMedicalEnrollments SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND MajorMedicalEnrollmentId = @MajorMedicalEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@MajorMedicalEnrollmentId", majorMedicalEnrollmentId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static MajorMedicalEnrollment ReadEnrollment(SqlDataReader reader) => new()
    {
        MajorMedicalEnrollmentId = reader.GetGuid("MajorMedicalEnrollmentId"),
        TenantId = reader.GetGuid("TenantId"),
        ClientId = reader.GetGuid("ClientId"),
        RecordedAt = reader.GetDateTimeOffset("RecordedAt"),
        IsActivePlan = reader.GetBoolean("IsActivePlan"),
        PlanName = reader.GetNullableString("PlanName"),
        CoverageStartDate = reader.GetNullableDateOnly("CoverageStartDate"),
        IsNewEnrollment = reader.GetBoolean("IsNewEnrollment"),
        HealthReimbursementArrangement = reader.GetBoolean("HealthReimbursementArrangement"),
        EnrollmentPlatform = reader.GetNullableString("EnrollmentPlatform"),
        EnrollmentLocation = reader.GetNullableString("EnrollmentLocation"),
        Notes = reader.GetNullableString("Notes"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };
}
