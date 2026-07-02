using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class ClientInteractionRepository : IClientInteractionRepository
{
    private readonly IDbExecutor _dbExecutor;

    public ClientInteractionRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<IReadOnlyList<ClientInteraction>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ClientInteractionId, TenantId, ClientId, ContactedAt, Summary, Notes, RequiresFollowUp,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.ClientInteractions
            WHERE ClientId = @ClientId AND IsDeleted = 0
            ORDER BY ContactedAt DESC;
            """;

        var items = new List<ClientInteraction>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadInteraction(reader));
                }
            },
            cancellationToken,
            new SqlParameter("@ClientId", clientId));

        return items;
    }

    public async Task<IReadOnlyList<FollowUpInteractionDto>> ListFollowUpsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT i.ClientInteractionId, i.ClientId, c.FirstName, c.LastName,
                   i.ContactedAt, i.Summary, i.RequiresFollowUp
            FROM dbo.ClientInteractions i
            INNER JOIN dbo.Clients c ON c.ClientId = i.ClientId
            WHERE i.IsDeleted = 0 AND c.IsDeleted = 0 AND i.RequiresFollowUp = 1
            ORDER BY i.ContactedAt ASC;
            """;

        var items = new List<FollowUpInteractionDto>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(new FollowUpInteractionDto
                    {
                        ClientInteractionId = reader.GetGuid("ClientInteractionId"),
                        ClientId = reader.GetGuid("ClientId"),
                        ClientFirstName = reader.GetNullableString("FirstName"),
                        ClientLastName = reader.GetNullableString("LastName"),
                        ContactedAt = reader.GetDateTimeOffset("ContactedAt"),
                        Summary = reader.GetNullableString("Summary"),
                        RequiresFollowUp = reader.GetBoolean("RequiresFollowUp"),
                    });
                }
            },
            cancellationToken);

        return items;
    }

    public async Task<ClientInteraction?> GetByIdAsync(
        Guid clientId,
        Guid clientInteractionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ClientInteractionId, TenantId, ClientId, ContactedAt, Summary, Notes, RequiresFollowUp,
                   CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
            FROM dbo.ClientInteractions
            WHERE ClientId = @ClientId AND ClientInteractionId = @ClientInteractionId AND IsDeleted = 0;
            """;

        ClientInteraction? interaction = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    interaction = ReadInteraction(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@ClientInteractionId", clientInteractionId));

        return interaction;
    }

    public async Task<ClientInteraction> InsertAsync(
        CreateClientInteractionModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var interactionId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.ClientInteractions (
                ClientInteractionId, TenantId, ClientId, ContactedAt, Summary, Notes, RequiresFollowUp,
                CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, IsDeleted)
            VALUES (
                @ClientInteractionId, @TenantId, @ClientId, @ContactedAt, @Summary, @Notes, @RequiresFollowUp,
                @CreatedAt, @CreatedByUserId, @UpdatedAt, @UpdatedByUserId, 0);
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientInteractionId", interactionId),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@ContactedAt", model.ContactedAt),
            new SqlParameter("@Summary", (object?)model.Summary ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@RequiresFollowUp", model.RequiresFollowUp),
            new SqlParameter("@CreatedAt", audit.Timestamp),
            new SqlParameter("@CreatedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return (await GetByIdAsync(model.ClientId, interactionId, cancellationToken))!;
    }

    public async Task<ClientInteraction?> UpdateAsync(
        UpdateClientInteractionModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ClientInteractions SET
                ContactedAt = @ContactedAt, Summary = @Summary, Notes = @Notes,
                RequiresFollowUp = @RequiresFollowUp,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND ClientInteractionId = @ClientInteractionId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@ClientInteractionId", model.ClientInteractionId),
            new SqlParameter("@ContactedAt", model.ContactedAt),
            new SqlParameter("@Summary", (object?)model.Summary ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
            new SqlParameter("@RequiresFollowUp", model.RequiresFollowUp),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows == 0
            ? null
            : await GetByIdAsync(model.ClientId, model.ClientInteractionId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid clientId,
        Guid clientInteractionId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ClientInteractions SET
                IsDeleted = 1, DeletedAt = @DeletedAt, DeletedByUserId = @DeletedByUserId,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND ClientInteractionId = @ClientInteractionId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@ClientInteractionId", clientInteractionId),
            new SqlParameter("@DeletedAt", audit.Timestamp),
            new SqlParameter("@DeletedByUserId", audit.UserId),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return rows > 0;
    }

    private static ClientInteraction ReadInteraction(SqlDataReader reader) => new()
    {
        ClientInteractionId = reader.GetGuid("ClientInteractionId"),
        TenantId = reader.GetGuid("TenantId"),
        ClientId = reader.GetGuid("ClientId"),
        ContactedAt = reader.GetDateTimeOffset("ContactedAt"),
        Summary = reader.GetNullableString("Summary"),
        Notes = reader.GetNullableString("Notes"),
        RequiresFollowUp = reader.GetBoolean("RequiresFollowUp"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };
}
