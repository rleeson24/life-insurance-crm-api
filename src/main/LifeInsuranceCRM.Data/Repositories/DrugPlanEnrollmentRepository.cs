using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class DrugPlanEnrollmentRepository : IDrugPlanEnrollmentRepository
{
    private readonly IDbExecutor _dbExecutor;

    public DrugPlanEnrollmentRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<DrugPlanEnrollment>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DrugPlanEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                   CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                   EnrollmentPlatform, EnrollmentLocation, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.DrugPlanEnrollments
            WHERE ClientId = @ClientId AND IsDeleted = 0
            ORDER BY RecordedAt DESC;
            """;

        var items = new List<DrugPlanEnrollment>();
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

    public async Task<DrugPlanEnrollment?> GetByIdAsync(
        Guid clientId,
        Guid drugPlanEnrollmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DrugPlanEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                   CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                   EnrollmentPlatform, EnrollmentLocation, Notes,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.DrugPlanEnrollments
            WHERE ClientId = @ClientId AND DrugPlanEnrollmentId = @DrugPlanEnrollmentId AND IsDeleted = 0;
            """;

        DrugPlanEnrollment? enrollment = null;
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
            new SqlParameter("@DrugPlanEnrollmentId", drugPlanEnrollmentId));

        return enrollment;
    }

    public async Task<DrugPlanEnrollment> InsertAsync(
        CreateDrugPlanEnrollmentModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var drugPlanEnrollmentId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.DrugPlanEnrollments (
                DrugPlanEnrollmentId, TenantId, ClientId, RecordedAt, IsActivePlan, PlanName,
                CoverageStartDate, IsNewEnrollment, HealthReimbursementArrangement,
                EnrollmentPlatform, EnrollmentLocation, Notes,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @DrugPlanEnrollmentId, @TenantId, @ClientId, @RecordedAt, @IsActivePlan, @PlanName,
                @CoverageStartDate, @IsNewEnrollment, @HealthReimbursementArrangement,
                @EnrollmentPlatform, @EnrollmentLocation, @Notes,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@DrugPlanEnrollmentId", drugPlanEnrollmentId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@IsActivePlan", model.IsActivePlan),
            new SqlParameter("@PlanName", (object?)model.PlanName ?? DBNull.Value),
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

        return (await GetByIdAsync(model.ClientId, drugPlanEnrollmentId, cancellationToken))!;
    }

    public async Task<DrugPlanEnrollment?> UpdateAsync(
        UpdateDrugPlanEnrollmentModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.DrugPlanEnrollments SET
                RecordedAt = @RecordedAt, IsActivePlan = @IsActivePlan, PlanName = @PlanName,
                CoverageStartDate = @CoverageStartDate,
                IsNewEnrollment = @IsNewEnrollment, HealthReimbursementArrangement = @HealthReimbursementArrangement,
                EnrollmentPlatform = @EnrollmentPlatform, EnrollmentLocation = @EnrollmentLocation, Notes = @Notes,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND DrugPlanEnrollmentId = @DrugPlanEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@DrugPlanEnrollmentId", model.DrugPlanEnrollmentId),
            new SqlParameter("@RecordedAt", model.RecordedAt),
            new SqlParameter("@IsActivePlan", model.IsActivePlan),
            new SqlParameter("@PlanName", (object?)model.PlanName ?? DBNull.Value),
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
            : await GetByIdAsync(model.ClientId, model.DrugPlanEnrollmentId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid clientId,
        Guid drugPlanEnrollmentId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.DrugPlanEnrollments SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND DrugPlanEnrollmentId = @DrugPlanEnrollmentId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@DrugPlanEnrollmentId", drugPlanEnrollmentId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static DrugPlanEnrollment ReadEnrollment(SqlDataReader reader) => new()
    {
        DrugPlanEnrollmentId = reader.GetGuid("DrugPlanEnrollmentId"),
        TenantId = reader.GetGuid("TenantId"),
        ClientId = reader.GetGuid("ClientId"),
        RecordedAt = reader.GetDateTimeOffset("RecordedAt"),
        IsActivePlan = reader.GetBoolean("IsActivePlan"),
        PlanName = reader.GetNullableString("PlanName"),
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
