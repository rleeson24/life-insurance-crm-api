using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Abstractions.Data;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class PlanNameRepository : IPlanNameRepository
{
    private readonly IDbExecutor _dbExecutor;

    public PlanNameRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<PlanName>> ListByYearAsync(
        PlanNameKind kind,
        short planYear,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            SELECT {table.IdColumn} AS PlanNameId, TenantId, PlanYear, Name,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM {table.TableName}
            WHERE PlanYear = @PlanYear AND IsDeleted = 0
            ORDER BY Name;
            """;

        return await ReadListAsync(sql, cancellationToken, new SqlParameter("@PlanYear", planYear));
    }

    public async Task<IReadOnlyList<PlanName>> ListByYearRangeAsync(
        PlanNameKind kind,
        short fromYear,
        short toYear,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            SELECT {table.IdColumn} AS PlanNameId, TenantId, PlanYear, Name,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM {table.TableName}
            WHERE PlanYear >= @FromYear AND PlanYear <= @ToYear AND IsDeleted = 0
            ORDER BY PlanYear DESC, Name;
            """;

        return await ReadListAsync(
            sql,
            cancellationToken,
            new SqlParameter("@FromYear", fromYear),
            new SqlParameter("@ToYear", toYear));
    }

    public async Task<PlanName?> GetByIdAsync(
        PlanNameKind kind,
        Guid planNameId,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            SELECT {table.IdColumn} AS PlanNameId, TenantId, PlanYear, Name,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM {table.TableName}
            WHERE {table.IdColumn} = @PlanNameId AND IsDeleted = 0;
            """;

        PlanName? item = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    item = ReadPlanName(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@PlanNameId", planNameId));

        return item;
    }

    public async Task<bool> ExistsByNameAsync(
        PlanNameKind kind,
        short planYear,
        string name,
        Guid? excludePlanNameId = null,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            SELECT TOP (1) 1
            FROM {table.TableName}
            WHERE PlanYear = @PlanYear AND Name = @Name AND IsDeleted = 0
              AND (@ExcludePlanNameId IS NULL OR {table.IdColumn} <> @ExcludePlanNameId);
            """;

        var result = await _dbExecutor.ExecuteScalarAsync<int>(
            sql,
            cancellationToken,
            new SqlParameter("@PlanYear", planYear),
            new SqlParameter("@Name", name),
            new SqlParameter("@ExcludePlanNameId", (object?)excludePlanNameId ?? DBNull.Value));

        return result == 1;
    }

    public async Task<int> CountByYearAsync(
        PlanNameKind kind,
        short planYear,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            SELECT COUNT(1)
            FROM {table.TableName}
            WHERE PlanYear = @PlanYear AND IsDeleted = 0;
            """;

        var count = await _dbExecutor.ExecuteScalarAsync<int>(
            sql,
            cancellationToken,
            new SqlParameter("@PlanYear", planYear));

        return count;
    }

    public async Task<PlanName> InsertAsync(
        PlanNameKind kind,
        Guid tenantId,
        short planYear,
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var planNameId = Guid.NewGuid();
        var table = PlanNameTable.For(kind);
        var sql = $"""
            INSERT INTO {table.TableName} (
                {table.IdColumn}, TenantId, PlanYear, Name,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @PlanNameId, @TenantId, @PlanYear, @Name,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@PlanNameId", planNameId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@PlanYear", planYear),
            new SqlParameter("@Name", name),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(kind, planNameId, cancellationToken))!;
    }

    public async Task<PlanName?> UpdateNameAsync(
        PlanNameKind kind,
        Guid planNameId,
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            UPDATE {table.TableName} SET
                Name = @Name,
                UpdatedAt = @UpdatedAt,
                UpdatedByUserId = @UpdatedByUserId
            WHERE {table.IdColumn} = @PlanNameId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@PlanNameId", planNameId),
            new SqlParameter("@Name", name),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0 ? null : await GetByIdAsync(kind, planNameId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        PlanNameKind kind,
        Guid planNameId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            UPDATE {table.TableName} SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE {table.IdColumn} = @PlanNameId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@PlanNameId", planNameId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    public async Task<IReadOnlyList<PlanName>> CloneYearAsync(
        PlanNameKind kind,
        Guid tenantId,
        short sourceYear,
        short targetYear,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var table = PlanNameTable.For(kind);
        var sql = $"""
            INSERT INTO {table.TableName} (
                {table.IdColumn}, TenantId, PlanYear, Name,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            OUTPUT
                INSERTED.{table.IdColumn} AS PlanNameId,
                INSERTED.TenantId,
                INSERTED.PlanYear,
                INSERTED.Name,
                INSERTED.CreatedAt,
                INSERTED.CreatedByUserId,
                INSERTED.UpdatedAt,
                INSERTED.UpdatedByUserId
            SELECT
                NEWID(), @TenantId, @TargetYear, s.Name,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0
            FROM {table.TableName} s
            WHERE s.PlanYear = @SourceYear AND s.IsDeleted = 0
              AND NOT EXISTS (
                  SELECT 1
                  FROM {table.TableName} t
                  WHERE t.PlanYear = @TargetYear
                    AND t.IsDeleted = 0
                    AND t.Name = s.Name);
            """;

        return await ReadListAsync(
            sql,
            cancellationToken,
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@SourceYear", sourceYear),
            new SqlParameter("@TargetYear", targetYear),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));
    }

    private async Task<IReadOnlyList<PlanName>> ReadListAsync(
        string sql,
        CancellationToken cancellationToken,
        params SqlParameter[] parameters)
    {
        var items = new List<PlanName>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadPlanName(reader));
                }
            },
            cancellationToken,
            parameters);

        return items;
    }

    private static PlanName ReadPlanName(Microsoft.Data.SqlClient.SqlDataReader reader) => new()
    {
        PlanNameId = reader.GetGuid("PlanNameId"),
        TenantId = reader.GetGuid("TenantId"),
        PlanYear = reader.GetInt16("PlanYear"),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };
}
