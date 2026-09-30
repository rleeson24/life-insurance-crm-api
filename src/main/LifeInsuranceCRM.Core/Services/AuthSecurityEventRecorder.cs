using System.Diagnostics;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.Core.Services;

public sealed class AuthSecurityEventRecorder : IAuthSecurityEventRecorder
{
    private readonly IAuthSecurityEventRepository _repository;
    private readonly IActorTracker _actorTracker;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INowProvider _nowProvider;
    private readonly ILogger<AuthSecurityEventRecorder> _logger;
    private readonly bool _trustForwardedFor;

    public AuthSecurityEventRecorder(
        IAuthSecurityEventRepository repository,
        IActorTracker actorTracker,
        IHttpContextAccessor httpContextAccessor,
        INowProvider nowProvider,
        ILogger<AuthSecurityEventRecorder> logger,
        IOptions<ClientIpOptions> clientIpOptions)
    {
        _repository = repository;
        _actorTracker = actorTracker;
        _httpContextAccessor = httpContextAccessor;
        _nowProvider = nowProvider;
        _logger = logger;
        _trustForwardedFor = clientIpOptions.Value.TrustForwardedFor;
    }

    public async Task RecordAsync(
        string eventType,
        bool success,
        string? failureReason = null,
        string? resource = null,
        CancellationToken cancellationToken = default,
        int? httpStatus = null,
        int? resultCount = null,
        Guid? targetId = null,
        string? detail = null)
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
            FailureReason = Truncate(PiiRedactor.Redact(failureReason), 256),
            IpAddress = Truncate(ClientIpAddress.Resolve(httpContext, _trustForwardedFor), 45),
            UserAgent = Truncate(httpContext?.Request.Headers.UserAgent.ToString(), 512),
            CorrelationId = Truncate(correlationId, 64),
            Resource = Truncate(resource ?? httpContext?.Request.Path.Value, 256),
            HttpStatus = httpStatus,
            ResultCount = resultCount,
            TargetId = targetId,
            Detail = Truncate(PiiRedactor.Redact(detail), 512),
        };

        try
        {
            await _repository.RecordAsync(securityEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never fail the request because audit insert failed; the log is the fallback record.
            _logger.LogError(ex, "Failed to persist security event {EventType}", eventType);
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
