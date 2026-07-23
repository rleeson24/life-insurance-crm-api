using System.Security.Claims;
using LifeInsuranceCRM.API.Middleware;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace LifeInsuranceCRM.API.Tests.Middleware;

public class ActorResolutionMiddlewareTests
{
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task InvokeAsync_WhenUserInactive_Returns403WithProblemDetails()
    {
        var context = CreateAuthenticatedContext();
        var actorTracker = new LifeInsuranceCRM.API.Auth.ActorTracker();
        var organizationUserRepository = new Mock<IOrganizationUserRepository>();
        organizationUserRepository
            .Setup(r => r.GetUserContextAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationUserContext(_tenantId, OrganizationRoles.Admin, IsActive: false));

        var securityEventRecorder = new Mock<IAuthSecurityEventRecorder>();
        var problemDetailsFactory = new ProblemDetailsFactory();
        var authOptions = Options.Create(new AuthOptions
        {
            UseDevelopmentAuthentication = true,
            DevelopmentTenantId = _tenantId,
        });

        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = new ActorResolutionMiddleware(next);
        await middleware.InvokeAsync(
            context,
            actorTracker,
            organizationUserRepository.Object,
            securityEventRecorder.Object,
            problemDetailsFactory,
            authOptions);

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserActive_SetsRoleOnActorTracker()
    {
        var context = CreateAuthenticatedContext();
        var actorTracker = new LifeInsuranceCRM.API.Auth.ActorTracker();
        var organizationUserRepository = new Mock<IOrganizationUserRepository>();
        organizationUserRepository
            .Setup(r => r.GetUserContextAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationUserContext(_tenantId, OrganizationRoles.Agent, IsActive: true));

        var securityEventRecorder = new Mock<IAuthSecurityEventRecorder>();
        var problemDetailsFactory = new ProblemDetailsFactory();
        var authOptions = Options.Create(new AuthOptions
        {
            UseDevelopmentAuthentication = true,
            DevelopmentTenantId = _tenantId,
        });

        string? capturedRole = null;
        Guid? capturedTenantId = null;
        var invoked = false;
        RequestDelegate next = _ =>
        {
            capturedRole = actorTracker.Role;
            capturedTenantId = actorTracker.TenantId;
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = new ActorResolutionMiddleware(next);
        await middleware.InvokeAsync(
            context,
            actorTracker,
            organizationUserRepository.Object,
            securityEventRecorder.Object,
            problemDetailsFactory,
            authOptions);

        Assert.True(invoked);
        Assert.Equal(OrganizationRoles.Agent, capturedRole);
        Assert.Equal(_tenantId, capturedTenantId);
    }

    private DefaultHttpContext CreateAuthenticatedContext()
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Email, "dev-user@localhost"),
            ],
            authenticationType: "Development");
        context.User = new ClaimsPrincipal(identity);
        context.Response.Body = new MemoryStream();
        return context;
    }
}
