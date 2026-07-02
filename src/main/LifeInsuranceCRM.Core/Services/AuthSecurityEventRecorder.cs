using System.Diagnostics;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace LifeInsuranceCRM.Core.Services;

public sealed class AuthSecurityEventRecorder : IAuthSecurityEventRecorder
{
    private readonly IAuthSecurityEventRepository _repository;
    private readonly IActorTracker _actorTracker;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INowProvider _nowProvider;

    public AuthSecurityEventRecorder(
        IAuthSecurityEventRepository repository,
        IActorTracker actorTracker,
        IHttpContextAccessor httpContextAccessor,
        INowProvider nowProvider)
    {
        _repository = repository;
        _actorTracker = actorTracker;
        _httpContextAccessor = httpContextAccessor;
        _nowProvider = nowProvider;
    }

    public async Task RecordAsync(
        string eventType,
        bool success,
        string? failureReason = null,
        string? resource = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId = Activity.Current?.TraceId.ToString()
            ?? httpContext?.TraceIdentifier;

        var securityEvent = new AuthSecurityEvent
        {
            AuthSecurityEventId = Guid.NewGuid(),
            TenantId = _actorTracker.TenantId,
            OccurredAt = _nowProvider.UtcNow,
            EventType = eventType,
            UserId = _actorTracker.UserId,
            UserEmail = _actorTracker.UserEmail,
            Success = success,
            FailureReason = Truncate(failureReason, 256),
            IpAddress = Truncate(httpContext?.Connection.RemoteIpAddress?.ToString(), 45),
            UserAgent = Truncate(httpContext?.Request.Headers.UserAgent.ToString(), 512),
            CorrelationId = Truncate(correlationId, 64),
            Resource = Truncate(resource ?? httpContext?.Request.Path.Value, 256),
        };

        try
        {
            await _repository.RecordAsync(securityEvent, cancellationToken);
        }
        catch
        {
            // Never fail the request because audit insert failed; telemetry still captures the trace.
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
