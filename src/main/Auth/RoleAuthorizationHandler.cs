using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace LifeInsuranceCRM.API.Auth;

public sealed class RoleRequirement : IAuthorizationRequirement
{
    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    public IReadOnlyList<string> AllowedRoles { get; }
}

public sealed class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IActorTracker _actorTracker;

    public RoleAuthorizationHandler(IActorTracker actorTracker)
    {
        _actorTracker = actorTracker;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (_actorTracker.Role is not null
            && requirement.AllowedRoles.Contains(_actorTracker.Role, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
