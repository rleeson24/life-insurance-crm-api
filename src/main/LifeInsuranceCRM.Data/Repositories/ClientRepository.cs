using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private const string ClientColumns = """
        ClientId, TenantId, FirstName, LastName, LegalName, HouseholdName, PrimaryPhone,
        AddressLine1, AddressLine2, City, State, PostalCode, EmailAddress, DateOfBirth,
        MedicareNumber, MedicarePartAEffectiveDate, MedicarePartBEffectiveDate,
        IsActive, IsAcaClient, HasContactConsent, Notes,
        CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId
        """;

    private readonly IDbExecutor _dbExecutor;

    public ClientRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<ListClientsResult> ListAsync(ListClientsRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : $"%{request.Search.Trim()}%";

        const string countSql = """
            SELECT COUNT(*)
            FROM dbo.Clients c
            WHERE c.IsDeleted = 0
              AND (@Search IS NULL OR c.FirstName LIKE @Search OR c.LastName LIKE @Search
                   OR c.LegalName LIKE @Search OR c.PrimaryPhone LIKE @Search
                   OR c.EmailAddress LIKE @Search OR c.MedicareNumber LIKE @Search)
              AND (@IsActive IS NULL OR c.IsActive = @IsActive)
              AND (@IsAcaClient IS NULL OR c.IsAcaClient = @IsAcaClient);
            """;

        const string listSql = """
            SELECT
                c.ClientId, c.FirstName, c.LastName, c.LegalName, c.PrimaryPhone,
                c.IsActive, c.IsAcaClient, c.UpdatedAt,
                (SELECT TOP 1 m.PlanName FROM dbo.MedicareEnrollments m
                 WHERE m.ClientId = c.ClientId AND m.IsDeleted = 0 AND m.IsActivePlan = 1
                 ORDER BY m.RecordedAt DESC) AS ActivePlanName,
                (SELECT TOP 1 i.ContactedAt FROM dbo.ClientInteractions i
                 WHERE i.ClientId = c.ClientId AND i.IsDeleted = 0
                 ORDER BY i.ContactedAt DESC) AS LastContactedAt
            FROM dbo.Clients c
            WHERE c.IsDeleted = 0
              AND (@Search IS NULL OR c.FirstName LIKE @Search OR c.LastName LIKE @Search
                   OR c.LegalName LIKE @Search OR c.PrimaryPhone LIKE @Search
                   OR c.EmailAddress LIKE @Search OR c.MedicareNumber LIKE @Search)
              AND (@IsActive IS NULL OR c.IsActive = @IsActive)
              AND (@IsAcaClient IS NULL OR c.IsAcaClient = @IsAcaClient)
            ORDER BY c.UpdatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new[]
        {
            new SqlParameter("@Search", (object?)searchPattern ?? DBNull.Value),
            new SqlParameter("@IsActive", (object?)request.IsActive ?? DBNull.Value),
            new SqlParameter("@IsAcaClient", (object?)request.IsAcaClient ?? DBNull.Value),
        };

        var totalCount = await _dbExecutor.ExecuteScalarAsync<int>(
            countSql,
            cancellationToken,
            parameters);

        var items = new List<ClientSummaryDto>();
        await _dbExecutor.ExecuteReaderAsync(
            listSql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(new ClientSummaryDto
                    {
                        ClientId = reader.GetGuid("ClientId"),
                        FirstName = reader.GetNullableString("FirstName"),
                        LastName = reader.GetNullableString("LastName"),
                        LegalName = reader.GetNullableString("LegalName"),
                        PrimaryPhone = reader.GetNullableString("PrimaryPhone"),
                        IsActive = reader.GetBoolean("IsActive"),
                        IsAcaClient = reader.GetBoolean("IsAcaClient"),
                        ActivePlanName = reader.GetNullableString("ActivePlanName"),
                        LastContactedAt = reader.GetNullableDateTimeOffset("LastContactedAt"),
                        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
                    });
                }
            },
            cancellationToken,
            parameters.Concat([
                new SqlParameter("@Offset", offset),
                new SqlParameter("@PageSize", pageSize),
            ]).ToArray());

        return new ListClientsResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {ClientColumns}
            FROM dbo.Clients
            WHERE ClientId = @ClientId AND IsDeleted = 0;
            """;

        Client? client = null;
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    client = ReadClient(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@ClientId", clientId));

        return client;
    }

    public async Task<Client> InsertAsync(
        CreateClientModel model,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var clientId = Guid.NewGuid();
        const string sql = """
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
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            BuildClientParameters(clientId, tenantId, model.FirstName, model.LastName, model.LegalName,
                model.HouseholdName, model.PrimaryPhone, model.AddressLine1, model.AddressLine2, model.City,
                model.State, model.PostalCode, model.EmailAddress, model.DateOfBirth, model.MedicareNumber,
                model.MedicarePartAEffectiveDate, model.MedicarePartBEffectiveDate, model.IsActive,
                model.IsAcaClient, model.HasContactConsent, model.Notes, audit));

        return (await GetByIdAsync(clientId, cancellationToken))!;
    }

    public async Task<Client?> UpdateAsync(
        UpdateClientModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Clients SET
                FirstName = @FirstName, LastName = @LastName, LegalName = @LegalName,
                HouseholdName = @HouseholdName, PrimaryPhone = @PrimaryPhone,
                AddressLine1 = @AddressLine1, AddressLine2 = @AddressLine2,
                City = @City, State = @State, PostalCode = @PostalCode,
                EmailAddress = @EmailAddress, DateOfBirth = @DateOfBirth,
                MedicareNumber = @MedicareNumber,
                MedicarePartAEffectiveDate = @MedicarePartAEffectiveDate,
                MedicarePartBEffectiveDate = @MedicarePartBEffectiveDate,
                IsActive = @IsActive, IsAcaClient = @IsAcaClient, HasContactConsent = @HasContactConsent,
                Notes = @Notes, UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND IsDeleted = 0;
            """;

        var rows = await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            BuildClientParameters(model.ClientId, null, model.FirstName, model.LastName, model.LegalName,
                model.HouseholdName, model.PrimaryPhone, model.AddressLine1, model.AddressLine2, model.City,
                model.State, model.PostalCode, model.EmailAddress, model.DateOfBirth, model.MedicareNumber,
                model.MedicarePartAEffectiveDate, model.MedicarePartBEffectiveDate, model.IsActive,
                model.IsAcaClient, model.HasContactConsent, model.Notes, audit));

        return rows == 0 ? null : await GetByIdAsync(model.ClientId, cancellationToken);
    }

    public async Task<Client?> UpdateStatusAsync(
        UpdateClientStatusModel model,
        AuditStamp audit,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(model.ClientId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        const string sql = """
            UPDATE dbo.Clients SET
                IsActive = @IsActive, IsAcaClient = @IsAcaClient, HasContactConsent = @HasContactConsent,
                UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE ClientId = @ClientId AND IsDeleted = 0;
            """;

        await _dbExecutor.ExecuteNonQueryAsync(
            sql,
            cancellationToken,
            new SqlParameter("@ClientId", model.ClientId),
            new SqlParameter("@IsActive", model.IsActive ?? existing.IsActive),
            new SqlParameter("@IsAcaClient", model.IsAcaClient ?? existing.IsAcaClient),
            new SqlParameter("@HasContactConsent", model.HasContactConsent ?? existing.HasContactConsent),
            new SqlParameter("@UpdatedAt", audit.Timestamp),
            new SqlParameter("@UpdatedByUserId", audit.UserId));

        return await GetByIdAsync(model.ClientId, cancellationToken);
    }

    private static Client ReadClient(SqlDataReader reader) => new()
    {
        ClientId = reader.GetGuid("ClientId"),
        TenantId = reader.GetGuid("TenantId"),
        FirstName = reader.GetNullableString("FirstName"),
        LastName = reader.GetNullableString("LastName"),
        LegalName = reader.GetNullableString("LegalName"),
        HouseholdName = reader.GetNullableString("HouseholdName"),
        PrimaryPhone = reader.GetNullableString("PrimaryPhone"),
        AddressLine1 = reader.GetNullableString("AddressLine1"),
        AddressLine2 = reader.GetNullableString("AddressLine2"),
        City = reader.GetNullableString("City"),
        State = reader.GetNullableString("State"),
        PostalCode = reader.GetNullableString("PostalCode"),
        EmailAddress = reader.GetNullableString("EmailAddress"),
        DateOfBirth = reader.GetNullableDateOnly("DateOfBirth"),
        MedicareNumber = reader.GetNullableString("MedicareNumber"),
        MedicarePartAEffectiveDate = reader.GetNullableDateOnly("MedicarePartAEffectiveDate"),
        MedicarePartBEffectiveDate = reader.GetNullableDateOnly("MedicarePartBEffectiveDate"),
        IsActive = reader.GetBoolean("IsActive"),
        IsAcaClient = reader.GetBoolean("IsAcaClient"),
        HasContactConsent = reader.GetBoolean("HasContactConsent"),
        Notes = reader.GetNullableString("Notes"),
        CreatedAt = reader.GetDateTimeOffset("CreatedAt"),
        CreatedByUserId = reader.GetGuid("CreatedByUserId"),
        UpdatedAt = reader.GetDateTimeOffset("UpdatedAt"),
        UpdatedByUserId = reader.GetGuid("UpdatedByUserId"),
    };

    private static SqlParameter[] BuildClientParameters(
        Guid clientId,
        Guid? tenantId,
        string? firstName,
        string? lastName,
        string? legalName,
        string? householdName,
        string? primaryPhone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? postalCode,
        string? emailAddress,
        DateOnly? dateOfBirth,
        string? medicareNumber,
        DateOnly? medicarePartAEffectiveDate,
        DateOnly? medicarePartBEffectiveDate,
        bool isActive,
        bool isAcaClient,
        bool hasContactConsent,
        string? notes,
        AuditStamp audit)
    {
        var parameters = new List<SqlParameter>
        {
            new("@ClientId", clientId),
            new("@FirstName", (object?)firstName ?? DBNull.Value),
            new("@LastName", (object?)lastName ?? DBNull.Value),
            new("@LegalName", (object?)legalName ?? DBNull.Value),
            new("@HouseholdName", (object?)householdName ?? DBNull.Value),
            new("@PrimaryPhone", (object?)primaryPhone ?? DBNull.Value),
            new("@AddressLine1", (object?)addressLine1 ?? DBNull.Value),
            new("@AddressLine2", (object?)addressLine2 ?? DBNull.Value),
            new("@City", (object?)city ?? DBNull.Value),
            new("@State", (object?)state ?? DBNull.Value),
            new("@PostalCode", (object?)postalCode ?? DBNull.Value),
            new("@EmailAddress", (object?)emailAddress ?? DBNull.Value),
            new("@DateOfBirth", dateOfBirth.HasValue ? dateOfBirth.Value : DBNull.Value),
            new("@MedicareNumber", (object?)medicareNumber ?? DBNull.Value),
            new("@MedicarePartAEffectiveDate", medicarePartAEffectiveDate.HasValue ? medicarePartAEffectiveDate.Value : DBNull.Value),
            new("@MedicarePartBEffectiveDate", medicarePartBEffectiveDate.HasValue ? medicarePartBEffectiveDate.Value : DBNull.Value),
            new("@IsActive", isActive),
            new("@IsAcaClient", isAcaClient),
            new("@HasContactConsent", hasContactConsent),
            new("@Notes", (object?)notes ?? DBNull.Value),
            new("@UpdatedAt", audit.Timestamp),
            new("@UpdatedByUserId", audit.UserId),
        };

        if (tenantId.HasValue)
        {
            parameters.Add(new SqlParameter("@TenantId", tenantId.Value));
            parameters.Add(new SqlParameter("@CreatedAt", audit.Timestamp));
            parameters.Add(new SqlParameter("@CreatedByUserId", audit.UserId));
        }

        return parameters.ToArray();
    }
}
