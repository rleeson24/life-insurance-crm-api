using System.Security.Claims;
using LifeInsuranceCRM.API.Auth;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace LifeInsuranceCRM.API.Tests.Auth;

public class RoleAuthorizationHandlerTests
{
    [Theory]
    [InlineData(OrganizationRoles.Admin, AuthorizationPolicies.CanRead, true)]
    [InlineData(OrganizationRoles.Agent, AuthorizationPolicies.CanRead, true)]
    [InlineData(OrganizationRoles.ReadOnly, AuthorizationPolicies.CanRead, true)]
    [InlineData(OrganizationRoles.Agent, AuthorizationPolicies.CanWrite, true)]
    [InlineData(OrganizationRoles.ReadOnly, AuthorizationPolicies.CanWrite, false)]
    [InlineData(OrganizationRoles.Agent, AuthorizationPolicies.CanDelete, false)]
    [InlineData(OrganizationRoles.Admin, AuthorizationPolicies.CanDelete, true)]
    public async Task HandleRequirementAsync_RespectsRolePolicy(string role, string policyName, bool shouldSucceed)
    {
        var actorTracker = new ActorTracker();
        actorTracker.SetActor(Guid.NewGuid(), "user@example.com", Guid.NewGuid(), role);

        var requirement = policyName switch
        {
            AuthorizationPolicies.CanRead => new RoleRequirement(
                OrganizationRoles.Admin,
                OrganizationRoles.Agent,
                OrganizationRoles.ReadOnly),
            AuthorizationPolicies.CanWrite => new RoleRequirement(
                OrganizationRoles.Admin,
                OrganizationRoles.Agent),
            AuthorizationPolicies.CanDelete => new RoleRequirement(OrganizationRoles.Admin),
            _ => throw new ArgumentOutOfRangeException(nameof(policyName)),
        };

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(),
            resource: null);

        var handler = new RoleAuthorizationHandler(actorTracker);
        await handler.HandleAsync(context);

        Assert.Equal(shouldSucceed, context.HasSucceeded);
    }
}
