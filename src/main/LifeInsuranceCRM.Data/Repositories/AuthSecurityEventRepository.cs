using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Entities;
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
}
