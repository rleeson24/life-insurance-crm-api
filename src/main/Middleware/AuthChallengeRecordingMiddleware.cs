using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.API.Middleware;

public sealed class AuthChallengeRecordingMiddleware
{
    private readonly RequestDelegate _next;

    public AuthChallengeRecordingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthSecurityEventRecorder securityEventRecorder)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await securityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.Unauthorized,
                success: false,
                failureReason: "Unauthorized",
                cancellationToken: context.RequestAborted);
        }
        else if (context.Response.StatusCode == StatusCodes.Status403Forbidden
                 && !context.Items.ContainsKey("TenantAccessDeniedRecorded"))
        {
            await securityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.Forbidden,
                success: false,
                failureReason: "Forbidden",
                cancellationToken: context.RequestAborted);
        }
    }
}
