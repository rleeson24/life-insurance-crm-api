using System.Security.Claims;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.API.Middleware;

public sealed class ActorResolutionMiddleware
{
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
        IOptions<AuthOptions> authOptions)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirstValue("oid")
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var email = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue("preferred_username");

                Guid? tenantId = null;

                if (authOptions.Value.UseDevelopmentAuthentication)
                {
                    tenantId = authOptions.Value.DevelopmentTenantId;
                }
                else
                {
                    tenantId = await organizationUserRepository.GetTenantIdForUserAsync(
                        userId,
                        context.RequestAborted);
                }

                if (tenantId.HasValue)
                {
                    actorTracker.SetActor(userId, email, tenantId.Value);
                    await securityEventRecorder.RecordAsync(
                        AuthSecurityEventTypes.TenantResolved,
                        success: true,
                        cancellationToken: context.RequestAborted);
                }
                else
                {
                    context.Items["TenantAccessDeniedRecorded"] = true;
                    await securityEventRecorder.RecordAsync(
                        AuthSecurityEventTypes.TenantAccessDenied,
                        success: false,
                        failureReason: "Tenant not found for user",
                        cancellationToken: context.RequestAborted);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
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
}
