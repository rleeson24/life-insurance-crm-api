using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class AuthSecurityEventRepository : IAuthSecurityEventRepository
{
    private const string InsertSql = """
        INSERT INTO dbo.AuthSecurityEvents (
            AuthSecurityEventId, TenantId, OccurredAt, EventType, UserId, UserEmail,
            Success, FailureReason, IpAddress, UserAgent, CorrelationId, Resource)
        VALUES (
            @AuthSecurityEventId, @TenantId, @OccurredAt, @EventType, @UserId, @UserEmail,
            @Success, @FailureReason, @IpAddress, @UserAgent, @CorrelationId, @Resource);
        """;

    private const string EventSelectColumns = """
        e.AuthSecurityEventId, e.TenantId, t.Name AS TenantName, e.OccurredAt, e.EventType,
        e.UserId, e.UserEmail, e.Success, e.FailureReason, e.IpAddress, e.UserAgent,
        e.CorrelationId, e.Resource
        """;

    private const string FilteredFromSql = """
        FROM dbo.AuthSecurityEvents e
        LEFT JOIN dbo.Tenants t ON t.TenantId = e.TenantId
        WHERE (@Search IS NULL
               OR e.UserEmail LIKE @Search
               OR e.EventType LIKE @Search
               OR e.FailureReason LIKE @Search
               OR e.Resource LIKE @Search
               OR e.IpAddress LIKE @Search
               OR t.Name LIKE @Search)
          AND (@EventType IS NULL OR e.EventType = @EventType)
          AND (@Success IS NULL OR e.Success = @Success)
        """;

    private readonly IDbExecutor _dbExecutor;

    public AuthSecurityEventRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public Task RecordAsync(AuthSecurityEvent securityEvent, CancellationToken cancellationToken = default) =>
        _dbExecutor.ExecuteNonQueryAsync(
            InsertSql,
            cancellationToken,
            new SqlParameter("@AuthSecurityEventId", securityEvent.AuthSecurityEventId),
            new SqlParameter("@TenantId", (object?)securityEvent.TenantId ?? DBNull.Value),
            new SqlParameter("@OccurredAt", securityEvent.OccurredAt),
            new SqlParameter("@EventType", securityEvent.EventType),
            new SqlParameter("@UserId", (object?)securityEvent.UserId ?? DBNull.Value),
            new SqlParameter("@UserEmail", (object?)securityEvent.UserEmail ?? DBNull.Value),
            new SqlParameter("@Success", securityEvent.Success),
            new SqlParameter("@FailureReason", (object?)securityEvent.FailureReason ?? DBNull.Value),
            new SqlParameter("@IpAddress", (object?)securityEvent.IpAddress ?? DBNull.Value),
            new SqlParameter("@UserAgent", (object?)securityEvent.UserAgent ?? DBNull.Value),
            new SqlParameter("@CorrelationId", (object?)securityEvent.CorrelationId ?? DBNull.Value),
            new SqlParameter("@Resource", (object?)securityEvent.Resource ?? DBNull.Value));

    public async Task<ListAuthSecurityEventsResult> ListAsync(
        ListAuthSecurityEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;
        var parameters = FilterParameters(request);

        using var bypass = _dbExecutor.BypassTenantFilter();
        var totalCount = await _dbExecutor.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(*)
            {FilteredFromSql};
            """,
            cancellationToken,
            parameters);

        var items = new List<AuthSecurityEventDto>();
        await _dbExecutor.ExecuteReaderAsync(
            $"""
            SELECT {EventSelectColumns}
            {FilteredFromSql}
            ORDER BY e.OccurredAt DESC, e.AuthSecurityEventId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadEvent(reader));
                }
            },
            cancellationToken,
            parameters.Concat([
                new SqlParameter("@Offset", offset),
                new SqlParameter("@PageSize", pageSize),
            ]).ToArray());

        return new ListAuthSecurityEventsResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AuthSecurityEventDto?> GetByIdAsync(
        Guid authSecurityEventId,
        CancellationToken cancellationToken = default)
    {
        AuthSecurityEventDto? securityEvent = null;
        using var bypass = _dbExecutor.BypassTenantFilter();
        await _dbExecutor.ExecuteReaderAsync(
            $"""
            SELECT {EventSelectColumns}
            FROM dbo.AuthSecurityEvents e
            LEFT JOIN dbo.Tenants t ON t.TenantId = e.TenantId
            WHERE e.AuthSecurityEventId = @AuthSecurityEventId;
            """,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    securityEvent = ReadEvent(reader);
                }
            },
            cancellationToken,
            new SqlParameter("@AuthSecurityEventId", authSecurityEventId));

        return securityEvent;
    }

    private static SqlParameter[] FilterParameters(ListAuthSecurityEventsRequest request)
    {
        string? searchPattern = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : $"%{request.Search.Trim()}%";
        var eventType = string.IsNullOrWhiteSpace(request.EventType)
            ? null
            : request.EventType.Trim();

        return
        [
            new SqlParameter("@Search", (object?)searchPattern ?? DBNull.Value),
            new SqlParameter("@EventType", (object?)eventType ?? DBNull.Value),
            new SqlParameter("@Success", (object?)request.Success ?? DBNull.Value),
        ];
    }

    private static AuthSecurityEventDto ReadEvent(SqlDataReader reader) => new()
    {
        AuthSecurityEventId = reader.GetGuid("AuthSecurityEventId"),
        TenantId = reader.GetNullableGuid("TenantId"),
        TenantName = reader.GetNullableString("TenantName"),
        OccurredAt = reader.GetDateTimeOffset("OccurredAt"),
        EventType = reader.GetString(reader.GetOrdinal("EventType")),
        UserId = reader.GetNullableGuid("UserId"),
        UserEmail = reader.GetNullableString("UserEmail"),
        Success = reader.GetBoolean("Success"),
        FailureReason = reader.GetNullableString("FailureReason"),
        IpAddress = reader.GetNullableString("IpAddress"),
        UserAgent = reader.GetNullableString("UserAgent"),
        CorrelationId = reader.GetNullableString("CorrelationId"),
        Resource = reader.GetNullableString("Resource"),
    };
}
