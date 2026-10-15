using System.Data;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class ImportMappingProfileRepository : IImportMappingProfileRepository
{
    private readonly IDbExecutor _dbExecutor;

    public ImportMappingProfileRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<ImportMappingProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ImportMappingProfileId, TenantId, Name, Definition,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.ImportMappingProfiles
            WHERE IsDeleted = 0
            ORDER BY Name;
            """;

        var items = new List<ImportMappingProfile>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadProfile(reader));
                }
            },
            cancellationToken);

        return items;
    }

    public async Task<ImportMappingProfile?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ImportMappingProfileId, TenantId, Name, Definition,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.ImportMappingProfiles
            WHERE Name = @Name AND IsDeleted = 0;
            """;

        ImportMappingProfile? item = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    item = ReadProfile(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@Name", name));

        return item;
    }

    public async Task<ImportMappingProfile> InsertAsync(
        Guid tenantId,
        string name,
        string definition,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.ImportMappingProfiles (
                ImportMappingProfileId, TenantId, Name, Definition,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @ImportMappingProfileId, @TenantId, @Name, @Definition,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ImportMappingProfileId", id),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Name", name),
            DefinitionParameter(definition),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByNameAsync(name, cancellationToken))!;
    }

    public async Task<ImportMappingProfile?> UpdateAsync(
        Guid importMappingProfileId,
        string definition,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ImportMappingProfiles SET
                Definition = @Definition,
                UpdatedAt = @UpdatedAt,
                UpdatedByUserId = @UpdatedByUserId
            WHERE ImportMappingProfileId = @ImportMappingProfileId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ImportMappingProfileId", importMappingProfileId),
            DefinitionParameter(definition),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        if (rows == 0)
        {
            return null;
        }

        const string readSql = """
            SELECT ImportMappingProfileId, TenantId, Name, Definition,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.ImportMappingProfiles
            WHERE ImportMappingProfileId = @ImportMappingProfileId AND IsDeleted = 0;
            """;

        ImportMappingProfile? item = null;
        await _dbExecutor.ExecuteReaderAsync(
            readSql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    item = ReadProfile(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@ImportMappingProfileId", importMappingProfileId));

        return item;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid importMappingProfileId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ImportMappingProfiles SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ImportMappingProfileId = @ImportMappingProfileId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ImportMappingProfileId", importMappingProfileId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static SqlParameter DefinitionParameter(string definition) =>
        new("@Definition", SqlDbType.NVarChar, -1) { Value = definition };

    private static ImportMappingProfile ReadProfile(SqlDataReader reader) => new()
    {
        ImportMappingProfileId = reader.GetGuid("ImportMappingProfileId"),
        TenantId = reader.GetGuid("TenantId"),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Definition = reader.GetString(reader.GetOrdinal("Definition")),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };
}
