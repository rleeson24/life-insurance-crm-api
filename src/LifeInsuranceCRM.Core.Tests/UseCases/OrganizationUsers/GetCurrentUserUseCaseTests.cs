using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.OrganizationUsers;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.OrganizationUsers;

public class GetCurrentUserUseCaseTests : UseCaseTestBase<GetCurrentUserUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly TenantDto _tenant;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<ITenantRepository> TenantRepository => MockFor<ITenantRepository>();

    public GetCurrentUserUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _tenant = new TenantDto
        {
            TenantId = _tenantId,
            Name = "Dust Insurance",
            IsActive = true,
            CreatedAt = CreateTimestamp(),
            UpdatedAt = CreateTimestamp(),
        };
    }

    protected override GetCurrentUserUseCase BuildSubject() =>
        new(ActorTracker.Object, TenantRepository.Object, new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenAuthenticated_ReturnsTenantName()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        ActorTracker.Setup(a => a.UserEmail).Returns("broker@dustinsurance.com");
        TenantRepository
            .Setup(r => r.GetByIdAsync(_tenantId, _ct))
            .ReturnsAsync(_tenant);

        var response = await BuildSubject().Execute(ProcessRequest<bool>.From(true, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(_userId, response.Result!.UserId);
        Assert.Equal(_tenantId, response.Result.TenantId);
        Assert.Equal("Dust Insurance", response.Result.TenantName);
        Assert.Equal("broker@dustinsurance.com", response.Result.Email);
        Assert.Equal(OrganizationRoles.Admin, response.Result.Role);
    }

    [Fact]
    public async Task Execute_WhenTenantMissing_ReturnsNullTenantName()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        TenantRepository
            .Setup(r => r.GetByIdAsync(_tenantId, _ct))
            .ReturnsAsync((TenantDto?)null);

        var response = await BuildSubject().Execute(ProcessRequest<bool>.From(true, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Null(response.Result!.TenantName);
    }

    [Fact]
    public async Task Execute_WhenUnauthenticated_ReturnsUnauthorized()
    {
        ActorTracker.SetupUnauthenticatedActor();

        var response = await BuildSubject().Execute(ProcessRequest<bool>.From(true, _ct));

        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Assert.Equal(ClientErrorCodes.ActorNotAuthenticated, response.ErrorCode);
        TenantRepository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
