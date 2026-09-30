using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LifeInsuranceCRM.API.Auth;

public static class JwtAuthenticationFailure
{
    public const string RecordedItemKey = "AuthTokenFailureRecorded";

    public static string EventTypeFor(Exception? exception) =>
        exception is SecurityTokenExpiredException
            ? AuthSecurityEventTypes.TokenExpired
            : AuthSecurityEventTypes.TokenValidationFailed;

    public static string ReasonFor(Exception? exception) =>
        exception is SecurityTokenExpiredException
            ? "Token expired"
            : "Token validation failed";

    public static async Task RecordAsync(AuthenticationFailedContext context)
    {
        context.HttpContext.Items[RecordedItemKey] = true;
        var recorder = context.HttpContext.RequestServices.GetService<IAuthSecurityEventRecorder>();
        if (recorder is null)
        {
            return;
        }

        await recorder.RecordAsync(
            EventTypeFor(context.Exception),
            success: false,
            failureReason: ReasonFor(context.Exception),
            resource: context.Request.Path.Value,
            cancellationToken: context.HttpContext.RequestAborted,
            httpStatus: StatusCodes.Status401Unauthorized);
    }
}
