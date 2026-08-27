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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        await CreateMiddleware(next).InvokeAsync(
            context,
            actorTracker,
            organizationUserRepository.Object,
            new Mock<IAuthSecurityEventRecorder>().Object,
            new ProblemDetailsFactory(),
            Options.Create(new AuthOptions { UseDevelopmentAuthentication = true, DevelopmentTenantId = _tenantId }),
            new ConfigurationBuilder().Build());

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenTenantInactive_Returns403()
    {
        var context = CreateAuthenticatedContext();
        var organizationUserRepository = new Mock<IOrganizationUserRepository>();
        organizationUserRepository
            .Setup(r => r.GetUserContextAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationUserContext(
                _tenantId,
                OrganizationRoles.Admin,
                IsActive: true,
                TenantIsActive: false));

        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        await CreateMiddleware(next).InvokeAsync(
            context,
            new LifeInsuranceCRM.API.Auth.ActorTracker(),
            organizationUserRepository.Object,
            new Mock<IAuthSecurityEventRecorder>().Object,
            new ProblemDetailsFactory(),
            Options.Create(new AuthOptions { UseDevelopmentAuthentication = true, DevelopmentTenantId = _tenantId }),
            new ConfigurationBuilder().Build());

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTenantInactiveAndSuperAdmin_Continues()
    {
        var context = CreateAuthenticatedContext();
        var actorTracker = new LifeInsuranceCRM.API.Auth.ActorTracker();
        var organizationUserRepository = new Mock<IOrganizationUserRepository>();
        organizationUserRepository
            .Setup(r => r.GetUserContextAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationUserContext(
                _tenantId,
                OrganizationRoles.SuperAdmin,
                IsActive: true,
                TenantIsActive: false));

        string? capturedRole = null;
        var invoked = false;
        RequestDelegate next = _ =>
        {
            capturedRole = actorTracker.Role;
            invoked = true;
            return Task.CompletedTask;
        };

        await CreateMiddleware(next).InvokeAsync(
            context,
            actorTracker,
            organizationUserRepository.Object,
            new Mock<IAuthSecurityEventRecorder>().Object,
            new ProblemDetailsFactory(),
            Options.Create(new AuthOptions { UseDevelopmentAuthentication = true, DevelopmentTenantId = _tenantId }),
            new ConfigurationBuilder().Build());

        Assert.True(invoked);
        Assert.Equal(OrganizationRoles.SuperAdmin, capturedRole);
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

        await CreateMiddleware(next).InvokeAsync(
            context,
            actorTracker,
            organizationUserRepository.Object,
            new Mock<IAuthSecurityEventRecorder>().Object,
            new ProblemDetailsFactory(),
            Options.Create(new AuthOptions { UseDevelopmentAuthentication = true, DevelopmentTenantId = _tenantId }),
            new ConfigurationBuilder().Build());

        Assert.True(invoked);
        Assert.Equal(OrganizationRoles.Agent, capturedRole);
        Assert.Equal(_tenantId, capturedTenantId);
    }

    [Fact]
    public async Task InvokeAsync_WhenOidAndPairwiseSub_UsesOid()
    {
        var context = CreateAuthenticatedContext(
            new Claim("oid", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, "WndTH1yWknoQ8QDJwLHJJ7vKrB7wNeQ0DXCvfVR5Jf8"),
            new Claim(ClaimTypes.Email, "dev-user@localhost"));
        var actorTracker = new LifeInsuranceCRM.API.Auth.ActorTracker();
        var organizationUserRepository = new Mock<IOrganizationUserRepository>();
        organizationUserRepository
            .Setup(r => r.GetUserContextAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationUserContext(_tenantId, OrganizationRoles.Admin, IsActive: true));

        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        await CreateMiddleware(next).InvokeAsync(
            context,
            actorTracker,
            organizationUserRepository.Object,
            new Mock<IAuthSecurityEventRecorder>().Object,
            new ProblemDetailsFactory(),
            Options.Create(new AuthOptions { UseDevelopmentAuthentication = true, DevelopmentTenantId = _tenantId }),
            new ConfigurationBuilder().Build());

        Assert.True(invoked);
        organizationUserRepository.Verify(
            r => r.GetUserContextAsync(_userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenPairwiseNameIdentifierOnly_Returns403()
    {
        var context = CreateAuthenticatedContext(
            new Claim(ClaimTypes.NameIdentifier, "WndTH1yWknoQ8QDJwLHJJ7vKrB7wNeQ0DXCvfVR5Jf8"),
            new Claim(ClaimTypes.Email, "dev-user@localhost"));
        var organizationUserRepository = new Mock<IOrganizationUserRepository>();

        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        await CreateMiddleware(next).InvokeAsync(
            context,
            new LifeInsuranceCRM.API.Auth.ActorTracker(),
            organizationUserRepository.Object,
            new Mock<IAuthSecurityEventRecorder>().Object,
            new ProblemDetailsFactory(),
            Options.Create(new AuthOptions()),
            new ConfigurationBuilder().Build());

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        organizationUserRepository.Verify(
            r => r.GetUserContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ActorResolutionMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ActorResolutionMiddleware>.Instance);

    private static DefaultHttpContext CreateAuthenticatedContext(params Claim[] claims)
    {
        var identityClaims = claims.Length == 0
            ?
            [
                new Claim(ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111"),
                new Claim(ClaimTypes.Email, "dev-user@localhost"),
            ]
            : claims;

        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(identityClaims, authenticationType: "Development");
        context.User = new ClaimsPrincipal(identity);
        context.Response.Body = new MemoryStream();
        return context;
    }
}
