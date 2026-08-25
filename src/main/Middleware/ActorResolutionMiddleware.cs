using System.Security.Claims;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.API.Middleware;

public sealed class ActorResolutionMiddleware
{
    private const string ObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private readonly RequestDelegate _next;

    public ActorResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IActorTracker actorTracker,
        IOrganizationUserRepository organizationUserRepository,
        IAuthSecurityEventRecorder securityEventRecorder,
        IProblemDetailsFactory problemDetailsFactory,
        IOptions<AuthOptions> authOptions,
        IConfiguration configuration)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (!TryGetEntraObjectId(context.User, out var userId))
            {
                await WriteForbiddenAsync(
                    context,
                    problemDetailsFactory,
                    securityEventRecorder,
                    AuthSecurityEventTypes.TenantAccessDenied,
                    "Token is missing a GUID oid claim. NameIdentifier/sub on personal Microsoft accounts is not a user id.",
                    cancellationToken: context.RequestAborted);
                return;
            }

            var email = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue("preferred_username");

            var userContext = await organizationUserRepository.GetUserContextAsync(
                userId,
                context.RequestAborted);

            if (userContext is null)
            {
                await WriteForbiddenAsync(
                    context,
                    problemDetailsFactory,
                    securityEventRecorder,
                    AuthSecurityEventTypes.TenantAccessDenied,
                    "Tenant not found for user",
                    cancellationToken: context.RequestAborted);
                return;
            }

            if (!userContext.IsActive)
            {
                await WriteForbiddenAsync(
                    context,
                    problemDetailsFactory,
                    securityEventRecorder,
                    AuthSecurityEventTypes.Forbidden,
                    "User account is inactive",
                    cancellationToken: context.RequestAborted);
                return;
            }

            var tenantId = authOptions.Value.ShouldUseDevelopmentScheme(configuration)
                ? authOptions.Value.DevelopmentTenantId
                : userContext.TenantId;

            actorTracker.SetActor(userId, email, tenantId, userContext.Role);
            await securityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.TenantResolved,
                success: true,
                cancellationToken: context.RequestAborted);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            actorTracker.Clear();
        }
    }

    internal static bool TryGetEntraObjectId(ClaimsPrincipal user, out Guid userId)
    {
        string?[] candidates =
        [
            user.FindFirstValue("oid"),
            user.FindFirstValue(ObjectIdClaimType),
            user.FindFirstValue(ClaimTypes.NameIdentifier),
        ];

        foreach (var candidate in candidates)
        {
            if (Guid.TryParse(candidate, out userId))
            {
                return true;
            }
        }

        userId = default;
        return false;
    }

    private static async Task WriteForbiddenAsync(
        HttpContext context,
        IProblemDetailsFactory problemDetailsFactory,
        IAuthSecurityEventRecorder securityEventRecorder,
        string eventType,
        string failureReason,
        CancellationToken cancellationToken)
    {
        context.Items["TenantAccessDeniedRecorded"] = true;
        await securityEventRecorder.RecordAsync(
            eventType,
            success: false,
            failureReason: failureReason,
            cancellationToken: cancellationToken);

        var problem = problemDetailsFactory.Create(
            context,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            failureReason,
            "access.forbidden");

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}
