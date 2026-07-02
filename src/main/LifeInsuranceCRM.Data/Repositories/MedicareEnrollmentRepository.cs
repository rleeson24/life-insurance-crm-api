using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class MedicareEnrollmentRepository : IMedicareEnrollmentRepository
{
    private readonly IDbExecutor _dbExecutor;

    public MedicareEnrollmentRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<MedicareEnrollment>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT MedicareEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                   PrescriptionDrugPlan, CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                   EnrollmentPlatform, EnrollmentLocation, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.MedicareEnrollments
            WHERE ClientId = @ClientId AND IsDeleted = 0
            ORDER BY RecordedAt DESC;
            """;

        var items = new List<MedicareEnrollment>();
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

    public async Task<MedicareEnrollment?> GetByIdAsync(
        Guid clientId,
        Guid medicareEnrollmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT MedicareEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                   PrescriptionDrugPlan, CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                   EnrollmentPlatform, EnrollmentLocation, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.MedicareEnrollments
            WHERE ClientId = @ClientId AND MedicareEnrollmentId = @MedicareEnrollmentId AND IsDeleted = 0;
            """;

        MedicareEnrollment? enrollment = null;
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
            new SqlParameter("@MedicareEnrollmentId", medicareEnrollmentId));

        return enrollment;
    }

    public async Task<MedicareEnrollment> InsertAsync(
        CreateMedicareEnrollmentModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var medicareEnrollmentId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.MedicareEnrollments (
                MedicareEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                PrescriptionDrugPlan, CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                EnrollmentPlatform, EnrollmentLocation, Notes,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @MedicareEnrollmentId, @TenantId, @ClientId, @RecordedAt, @IsActivePlan, @PlanName,
                @PrescriptionDrugPlan, @CoverageStartDate, @IsNewEnrollment, @HealthReimbursementArrangement,
                @EnrollmentPlatform, @EnrollmentLocation, @Notes,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@MedicareEnrollmentId", medicareEnrollmentId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@IsActivePlan", model.IsActivePlan),
            new SqlParameter("@PlanName", (object?)model.PlanName ?? DBNull.Value),
            new SqlParameter("@PrescriptionDrugPlan", (object?)model.PrescriptionDrugPlan ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsNewEnrollment", model.IsNewEnrollment),
            new SqlParameter("@HealthReimbursementArrangement", (object?)model.HealthReimbursementArrangement ?? DBNull.Value),
            new SqlParameter("@EnrollmentPlatform", (object?)model.EnrollmentPlatform ?? DBNull.Value),
            new SqlParameter("@EnrollmentLocation", (object?)model.EnrollmentLocation ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(model.ClientId, medicareEnrollmentId, cancellationToken))!;
    }

    public async Task<MedicareEnrollment?> UpdateAsync(
        UpdateMedicareEnrollmentModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.MedicareEnrollments SET
                RecordedAt = @RecordedAt, IsActivePlan = @IsActivePlan, PlanName = @PlanName,
                PrescriptionDrugPlan = @PrescriptionDrugPlan, CoverageStartDate = @CoverageStartDate,
                IsNewEnrollment = @IsNewEnrollment, HealthReimbursementArrangement = @HealthReimbursementArrangement,
                EnrollmentPlatform = @EnrollmentPlatform, EnrollmentLocation = @EnrollmentLocation, Notes = @Notes,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND MedicareEnrollmentId = @MedicareEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@MedicareEnrollmentId", model.MedicareEnrollmentId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@IsActivePlan", model.IsActivePlan),
            new SqlParameter("@PlanName", (object?)model.PlanName ?? DBNull.Value),
            new SqlParameter("@PrescriptionDrugPlan", (object?)model.PrescriptionDrugPlan ?? DBNull.Value),
            new SqlParameter("@CoverageStartDate", model.CoverageStartDate.HasValue ? model.CoverageStartDate.Value : DBNull.Value),
            new SqlParameter("@IsNewEnrollment", model.IsNewEnrollment),
            new SqlParameter("@HealthReimbursementArrangement", (object?)model.HealthReimbursementArrangement ?? DBNull.Value),
            new SqlParameter("@EnrollmentPlatform", (object?)model.EnrollmentPlatform ?? DBNull.Value),
            new SqlParameter("@EnrollmentLocation", (object?)model.EnrollmentLocation ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(model.ClientId, model.MedicareEnrollmentId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid clientId,
        Guid medicareEnrollmentId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.MedicareEnrollments SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND MedicareEnrollmentId = @MedicareEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@MedicareEnrollmentId", medicareEnrollmentId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static MedicareEnrollment ReadEnrollment(SqlDataReader reader) => new()
    {
        MedicareEnrollmentId = reader.GetGuid("MedicareEnrollmentId"),
        TenantId = reader.GetGuid("TenantId"),
        ClientId = reader.GetGuid("ClientId"),
        RecordedAt = reader.GetDateTimeOffset("RecordedAt"),
        IsActivePlan = reader.GetBoolean("IsActivePlan"),
        PlanName = reader.GetNullableString("PlanName"),
        PrescriptionDrugPlan = reader.GetNullableString("PrescriptionDrugPlan"),
        CoverageStartDate = reader.GetNullableDateOnly("CoverageStartDate"),
        IsNewEnrollment = reader.GetBoolean("IsNewEnrollment"),
        HealthReimbursementArrangement = reader.GetNullableString("HealthReimbursementArrangement"),
        EnrollmentPlatform = reader.GetNullableString("EnrollmentPlatform"),
        EnrollmentLocation = reader.GetNullableString("EnrollmentLocation"),
        Notes = reader.GetNullableString("Notes"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };
}
