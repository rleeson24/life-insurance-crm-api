using System.Net;
using System.Net.Http.Json;
using System.Text;
using LifeInsuranceCRM.API.Models;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace LifeInsuranceCRM.API.Tests.Http;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public Guid DevelopmentUserId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid DevelopmentTenantId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly string? _previousUseDevAuth;
    private readonly string? _previousAzureAdClientId;
    private readonly string? _previousAzureAdTenantId;

    public ApiWebApplicationFactory()
    {
        _previousUseDevAuth = Environment.GetEnvironmentVariable("Auth__UseDevelopmentAuthentication");
        _previousAzureAdClientId = Environment.GetEnvironmentVariable("AzureAd__ClientId");
        _previousAzureAdTenantId = Environment.GetEnvironmentVariable("AzureAd__TenantId");
        Environment.SetEnvironmentVariable("Auth__UseDevelopmentAuthentication", "true");
        Environment.SetEnvironmentVariable("AzureAd__ClientId", "");
        Environment.SetEnvironmentVariable("AzureAd__TenantId", "");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOrganizationUserRepository>();
            var organizationUsers = new Mock<IOrganizationUserRepository>();
            organizationUsers
                .Setup(r => r.GetUserContextAsync(DevelopmentUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrganizationUserContext(
                    DevelopmentTenantId,
                    OrganizationRoles.Admin,
                    IsActive: true));
            services.AddSingleton(organizationUsers.Object);

            services.RemoveAll<IAuthSecurityEventRecorder>();
            var securityEvents = new Mock<IAuthSecurityEventRecorder>();
            securityEvents
                .Setup(r => r.RecordAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(securityEvents.Object);
        });
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("Auth__UseDevelopmentAuthentication", _previousUseDevAuth);
        Environment.SetEnvironmentVariable("AzureAd__ClientId", _previousAzureAdClientId);
        Environment.SetEnvironmentVariable("AzureAd__TenantId", _previousAzureAdTenantId);
        base.Dispose(disposing);
    }
}

public class InvalidRequestBodyTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InvalidRequestBodyTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostClients_EmptyBody_Returns400WithRequestInvalid()
    {
        var response = await _client.PostAsync(
            "/api/clients",
            new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        await AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task PostClients_MalformedJson_Returns400WithRequestInvalid()
    {
        var response = await _client.PostAsync(
            "/api/clients",
            new StringContent("{", Encoding.UTF8, "application/json"));

        await AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task PostClients_FirstNameAsNumber_Returns400WithRequestInvalid()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            new Dictionary<string, object?>
            {
                ["firstName"] = 123,
                ["lastName"] = "Smith",
            });

        await AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task PostClients_LastNameAsBoolean_Returns400WithRequestInvalid()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            new Dictionary<string, object?>
            {
                ["firstName"] = "Jane",
                ["lastName"] = true,
            });

        await AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task PostClients_BodyAsArray_Returns400WithRequestInvalid()
    {
        var response = await _client.PostAsync(
            "/api/clients",
            new StringContent("[]", Encoding.UTF8, "application/json"));

        await AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task GetProductionReport_PlanYearAsString_Returns400WithRequestInvalid()
    {
        var response = await _client.GetAsync("/api/reports/production?planYear=not-a-number");

        await AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task GetProductionReport_PlanYearOverflow_Returns400WithRequestInvalid()
    {
        var response = await _client.GetAsync($"/api/reports/production?planYear={int.MaxValue}");

        await AssertInvalidRequestAsync(response);
    }

    private static async Task AssertInvalidRequestAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
        Assert.NotNull(problem);
        Assert.Equal("request.invalid", problem.ErrorCode);
    }
}
