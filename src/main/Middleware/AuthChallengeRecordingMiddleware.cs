using LifeInsuranceCRM.API.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Services;

namespace LifeInsuranceCRM.API.Middleware;

public sealed class AuthChallengeRecordingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthChallengeRecordingMiddleware> _logger;

    public AuthChallengeRecordingMiddleware(
        RequestDelegate next,
        ILogger<AuthChallengeRecordingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthSecurityEventRecorder securityEventRecorder)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized
            && !context.Items.ContainsKey(JwtAuthenticationFailure.RecordedItemKey))
        {
            _logger.LogWarning(
                "Unauthorized request {HttpMethod} {Path}",
                context.Request.Method,
                context.Request.Path.Value);

            await securityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.Unauthorized,
                success: false,
                failureReason: "Unauthorized",
                cancellationToken: context.RequestAborted,
                httpStatus: SecurityAudit.StatusUnauthorized);
        }
        else if (context.Response.StatusCode == StatusCodes.Status403Forbidden
                 && !context.Items.ContainsKey("TenantAccessDeniedRecorded"))
        {
            _logger.LogWarning(
                "Forbidden request {HttpMethod} {Path}",
                context.Request.Method,
                context.Request.Path.Value);

            await securityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.Forbidden,
                success: false,
                failureReason: "Forbidden",
                cancellationToken: context.RequestAborted,
                httpStatus: SecurityAudit.StatusForbidden);
        }
    }
}
