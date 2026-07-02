using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;

namespace LifeInsuranceCRM.API.Middleware;

public sealed class TenantSessionContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantSessionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IActorTracker actorTracker, IDbExecutor dbExecutor)
    {
        if (actorTracker.TenantId is Guid tenantId)
        {
            await dbExecutor.SetTenantSessionContextAsync(tenantId, context.RequestAborted);
        }

        await _next(context);
    }
}
