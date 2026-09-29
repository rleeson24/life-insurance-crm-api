using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.AuthSecurityEvents;

public class ListAuthSecurityEventsUseCaseTests : UseCaseTestBase<ListAuthSecurityEventsUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly ListAuthSecurityEventsRequest _request;
    private readonly ListAuthSecurityEventsResult _listResult;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IAuthSecurityEventRepository> Repository => MockFor<IAuthSecurityEventRepository>();

    public ListAuthSecurityEventsUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _request = new ListAuthSecurityEventsRequest { Page = 1, PageSize = 50 };
        _listResult = new ListAuthSecurityEventsResult
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 50,
        };
    }

    protected override ListAuthSecurityEventsUseCase BuildSubject() =>
        new(ActorTracker.Object, Repository.Object, new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenAdmin_ReturnsForbidden()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);

        var response = await BuildSubject().Execute(
            ProcessRequest<ListAuthSecurityEventsRequest>.From(_request, _ct));

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(TenantErrorCodes.ActorNotSuperAdmin, response.ErrorCode);
        Repository.Verify(
            r => r.ListAsync(It.IsAny<ListAuthSecurityEventsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_ReturnsEvents()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        Repository.Setup(r => r.ListAsync(_request, _ct)).ReturnsAsync(_listResult);

        var response = await BuildSubject().Execute(
            ProcessRequest<ListAuthSecurityEventsRequest>.From(_request, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Same(_listResult, response.Result);
    }
}
