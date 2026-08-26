using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.OrganizationUsers;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.OrganizationUsers;

public class ListOrganizationUsersUseCaseTests : UseCaseTestBase<ListOrganizationUsersUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IOrganizationUserRepository> OrganizationUserRepository => MockFor<IOrganizationUserRepository>();

    public ListOrganizationUsersUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
    }

    protected override ListOrganizationUsersUseCase BuildSubject() =>
        new(ActorTracker.Object, OrganizationUserRepository.Object, new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenAdmin_ListsOwnTenantWithoutSuperAdmins()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        OrganizationUserRepository
            .Setup(r => r.ListAsync(_tenantId, false, _ct))
            .ReturnsAsync(Array.Empty<OrganizationUserDto>());

        var response = await BuildSubject().Execute(
            ProcessRequest<ListOrganizationUsersRequest>.From(
                new ListOrganizationUsersRequest { TenantId = CreateGuid() },
                _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        OrganizationUserRepository.Verify(r => r.ListAsync(_tenantId, false, _ct), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_UsesTenantFilterAndIncludesSuperAdmins()
    {
        var filterTenantId = CreateGuid();
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        OrganizationUserRepository
            .Setup(r => r.ListAsync(filterTenantId, true, _ct))
            .ReturnsAsync(Array.Empty<OrganizationUserDto>());

        var response = await BuildSubject().Execute(
            ProcessRequest<ListOrganizationUsersRequest>.From(
                new ListOrganizationUsersRequest { TenantId = filterTenantId },
                _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        OrganizationUserRepository.Verify(r => r.ListAsync(filterTenantId, true, _ct), Times.Once);
    }
}
