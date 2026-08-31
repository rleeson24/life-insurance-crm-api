using System.Diagnostics;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Import;
using LifeInsuranceCRM.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class AccessImportRepository : IAccessImportRepository
{
    private readonly DatabaseOptions _options;
    private readonly IActorTracker _actorTracker;

    public AccessImportRepository(IOptions<DatabaseOptions> options, IActorTracker actorTracker)
    {
        _options = options.Value;
        _actorTracker = actorTracker;
    }

    public async Task<AccessImportPersistResult> ImportAsync(
        MappedAccessImport mapped,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        MarkCurrentSpanContainsPhiSql();
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await ApplyTenantSessionContextAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var lockResult = await ExecuteScalarAsync<int>(
                connection,
                transaction,
                """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = @Resource,
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Transaction',
                    @LockTimeout = 15000;
                SELECT @result;
                """,
                cancellationToken,
                new SqlParameter("@Resource", $"access-import:{tenantId:D}"));

            if (lockResult < 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AccessImportPersistResult { LockNotAcquired = true };
            }

            var existingClients = await ExecuteScalarAsync<int>(
                connection,
                transaction,
                "SELECT COUNT(1) FROM dbo.Clients WHERE IsDeleted = 0;",
                cancellationToken);

            if (existingClients > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AccessImportPersistResult { TenantAlreadyHasClients = true };
            }

            foreach (var client in mapped.Clients)
            {
                await ExecuteNonQueryAsync(
                    connection,
                    transaction,
                    """
                    INSERT INTO dbo.Clients (
                        ClientId, TenantId, FirstName, LastName, LegalName, HouseholdName, PrimaryPhone,
                        AddressLine1, AddressLine2, City, State, PostalCode, EmailAddress, DateOfBirth,
                        MedicareNumber, MedicarePartAEffectiveDate, MedicarePartBEffectiveDate,
                        IsActive, IsAcaClient, HasContactConsent, Notes,
                        CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
                    VALUES (
                        @ClientId, @TenantId, @FirstName, @LastName, @LegalName, @HouseholdName, @PrimaryPhone,
                        @AddressLine1, @AddressLine2, @City, @State, @PostalCode, @EmailAddress, @DateOfBirth,
                        @MedicareNumber, @MedicarePartAEffectiveDate, @MedicarePartBEffectiveDate,
                        @IsActive, @IsAcaClient, @HasContactConsent, @Notes,
                        @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
                    """,
                    cancellationToken,
                    Param("@ClientId", client.ClientId),
                    Param("@TenantId", tenantId),
                    Param("@FirstName", client.FirstName),
                    Param("@LastName", client.LastName),
                    Param("@LegalName", client.LegalName),
                    Param("@HouseholdName", client.HouseholdName),
                    Param("@PrimaryPhone", client.PrimaryPhone),
                    Param("@AddressLine1", client.AddressLine1),
                    Param("@AddressLine2", client.AddressLine2),
                    Param("@City", client.City),
                    Param("@State", client.State),
                    Param("@PostalCode", client.PostalCode),
                    Param("@EmailAddress", client.EmailAddress),
                    DateParam("@DateOfBirth", client.DateOfBirth),
                    Param("@MedicareNumber", client.MedicareNumber),
                    DateParam("@MedicarePartAEffectiveDate", client.MedicarePartAEffectiveDate),
                    DateParam("@MedicarePartBEffectiveDate", client.MedicarePartBEffectiveDate),
                    Param("@IsActive", client.IsActive),
                    Param("@IsAcaClient", client.IsAcaClient),
                    Param("@HasContactConsent", client.HasContactConsent),
                    Param("@Notes", client.Notes),
                    Param("@CreatedAt", audit.Timestamp),
                    Param("@CreatedByUserId", audit.UserId),
                    Param("@UpdatedAt", audit.Timestamp),
                    Param("@UpdatedByUserId", audit.UserId));
            }

            foreach (var enrollment in mapped.MajorMedicalEnrollments)
            {
                await InsertMajorMedicalAsync(connection, transaction, tenantId, audit, enrollment, cancellationToken);
            }

            foreach (var enrollment in mapped.DrugPlanEnrollments)
            {
                await InsertDrugPlanAsync(connection, transaction, tenantId, audit, enrollment, cancellationToken);
            }

            foreach (var enrollment in mapped.SecondaryEnrollments)
            {
                await ExecuteNonQueryAsync(
                    connection,
                    transaction,
                    """
                    INSERT INTO dbo.SecondaryEnrollments (
                        SecondaryEnrollmentId, TenantId, ClientId, RecordedAt, PlanOrCarrierName,
                        CoverageStartDate, IsActiveCoverage, Notes,
                        CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
                    VALUES (
                        @SecondaryEnrollmentId, @TenantId, @ClientId, @RecordedAt, @PlanOrCarrierName,
                        @CoverageStartDate, @IsActiveCoverage, @Notes,
                        @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
                    """,
                    cancellationToken,
                    Param("@SecondaryEnrollmentId", enrollment.SecondaryEnrollmentId),
                    Param("@TenantId", tenantId),
                    Param("@ClientId", enrollment.ClientId),
                    Param("@RecordedAt", enrollment.RecordedAt),
                    Param("@PlanOrCarrierName", enrollment.PlanOrCarrierName),
                    DateParam("@CoverageStartDate", enrollment.CoverageStartDate),
                    Param("@IsActiveCoverage", enrollment.IsActiveCoverage),
                    Param("@Notes", enrollment.Notes),
                    Param("@CreatedAt", audit.Timestamp),
                    Param("@CreatedByUserId", audit.UserId),
                    Param("@UpdatedAt", audit.Timestamp),
                    Param("@UpdatedByUserId", audit.UserId));
            }

            foreach (var interaction in mapped.Interactions)
            {
                await ExecuteNonQueryAsync(
                    connection,
                    transaction,
                    """
                    INSERT INTO dbo.ClientInteractions (
                        ClientInteractionId, TenantId, ClientId, ContactedAt, Summary, Notes, RequiresFollowUp,
                        CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
                    VALUES (
                        @ClientInteractionId, @TenantId, @ClientId, @ContactedAt, @Summary, @Notes, @RequiresFollowUp,
                        @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
                    """,
                    cancellationToken,
                    Param("@ClientInteractionId", interaction.ClientInteractionId),
                    Param("@TenantId", tenantId),
                    Param("@ClientId", interaction.ClientId),
                    Param("@ContactedAt", interaction.ContactedAt),
                    Param("@Summary", interaction.Summary),
                    Param("@Notes", interaction.Notes),
                    Param("@RequiresFollowUp", interaction.RequiresFollowUp),
                    Param("@CreatedAt", audit.Timestamp),
                    Param("@CreatedByUserId", audit.UserId),
                    Param("@UpdatedAt", audit.Timestamp),
                    Param("@UpdatedByUserId", audit.UserId));
            }

            var medicareNames = 0;
            var drugNames = 0;
            var secondaryNames = 0;
            foreach (var planName in mapped.PlanNames)
            {
                var inserted = await InsertPlanNameIfMissingAsync(
                    connection,
                    transaction,
                    tenantId,
                    audit,
                    planName,
                    cancellationToken);
                if (inserted)
                {
                    switch (planName.Kind)
                    {
                        case PlanNameKind.Medicare:
                            medicareNames++;
                            break;
                        case PlanNameKind.Drug:
                            drugNames++;
                            break;
                        case PlanNameKind.Secondary:
                            secondaryNames++;
                            break;
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return new AccessImportPersistResult
            {
                MedicarePlanNamesInserted = medicareNames,
                DrugPlanNamesInserted = drugNames,
                SecondaryPlanNamesInserted = secondaryNames,
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task InsertMajorMedicalAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid tenantId,
        AuditStamp audit,
        MappedImportMajorMedicalEnrollment enrollment,
        CancellationToken cancellationToken) =>
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
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
            """,
            cancellationToken,
            Param("@MajorMedicalEnrollmentId", enrollment.MajorMedicalEnrollmentId),
            Param("@TenantId", tenantId),
            Param("@ClientId", enrollment.ClientId),
            Param("@RecordedAt", enrollment.RecordedAt),
            Param("@IsActivePlan", enrollment.IsActivePlan),
            Param("@PlanName", enrollment.PlanName),
            DateParam("@CoverageStartDate", enrollment.CoverageStartDate),
            Param("@IsNewEnrollment", enrollment.IsNewEnrollment),
            Param("@HealthReimbursementArrangement", enrollment.HealthReimbursementArrangement),
            Param("@EnrollmentPlatform", enrollment.EnrollmentPlatform),
            Param("@EnrollmentLocation", enrollment.EnrollmentLocation),
            Param("@Notes", enrollment.Notes),
            Param("@CreatedAt", audit.Timestamp),
            Param("@CreatedByUserId", audit.UserId),
            Param("@UpdatedAt", audit.Timestamp),
            Param("@UpdatedByUserId", audit.UserId));

    private static async Task InsertDrugPlanAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid tenantId,
        AuditStamp audit,
        MappedImportDrugPlanEnrollment enrollment,
        CancellationToken cancellationToken) =>
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
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
            """,
            cancellationToken,
            Param("@DrugPlanEnrollmentId", enrollment.DrugPlanEnrollmentId),
            Param("@TenantId", tenantId),
            Param("@ClientId", enrollment.ClientId),
            Param("@RecordedAt", enrollment.RecordedAt),
            Param("@IsActivePlan", enrollment.IsActivePlan),
            Param("@PlanName", enrollment.PlanName),
            DateParam("@CoverageStartDate", enrollment.CoverageStartDate),
            Param("@IsNewEnrollment", enrollment.IsNewEnrollment),
            Param("@HealthReimbursementArrangement", enrollment.HealthReimbursementArrangement),
            Param("@EnrollmentPlatform", enrollment.EnrollmentPlatform),
            Param("@EnrollmentLocation", enrollment.EnrollmentLocation),
            Param("@Notes", enrollment.Notes),
            Param("@CreatedAt", audit.Timestamp),
            Param("@CreatedByUserId", audit.UserId),
            Param("@UpdatedAt", audit.Timestamp),
            Param("@UpdatedByUserId", audit.UserId));

    private static async Task<bool> InsertPlanNameIfMissingAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid tenantId,
        AuditStamp audit,
        MappedImportPlanName planName,
        CancellationToken cancellationToken)
    {
        var table = PlanNameTable.For(planName.Kind);
        var rows = await ExecuteNonQueryAsync(
            connection,
            transaction,
            $"""
            INSERT INTO {table.TableName} (
                {table.IdColumn}, TenantId, PlanYear, Name,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            SELECT
                @PlanNameId, @TenantId, @PlanYear, @Name,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0
            WHERE NOT EXISTS (
                SELECT 1
                FROM {table.TableName}
                WHERE PlanYear = @PlanYear AND Name = @Name AND IsDeleted = 0);
            """,
            cancellationToken,
            Param("@PlanNameId", Guid.NewGuid()),
            Param("@TenantId", tenantId),
            Param("@PlanYear", planName.PlanYear),
            Param("@Name", planName.Name),
            Param("@CreatedAt", audit.Timestamp),
            Param("@CreatedByUserId", audit.UserId),
            Param("@UpdatedAt", audit.Timestamp),
            Param("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private async Task ApplyTenantSessionContextAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (_actorTracker.TenantId is not Guid tenantId)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            "EXEC sys.sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;";
        command.Parameters.Add(new SqlParameter("@tenantId", tenantId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params SqlParameter[] parameters)
    {
        await using var command = CreateCommand(connection, transaction, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params SqlParameter[] parameters)
    {
        await using var command = CreateCommand(connection, transaction, sql, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return default!;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        SqlParameter[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = AccessImportLimits.CommandTimeoutSeconds;
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        return command;
    }

    private static SqlParameter Param(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    private static SqlParameter DateParam(string name, DateOnly? value) =>
        new(name, value.HasValue ? value.Value : DBNull.Value);

    private static void MarkCurrentSpanContainsPhiSql() =>
        Activity.Current?.SetTag(TelemetryConstants.ContainsPhiSqlTag, true);
}
