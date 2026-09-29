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

public class GetAuthSecurityEventUseCaseTests : UseCaseTestBase<GetAuthSecurityEventUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _eventId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly AuthSecurityEventDto _securityEvent;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IAuthSecurityEventRepository> Repository => MockFor<IAuthSecurityEventRepository>();

    public GetAuthSecurityEventUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _eventId = CreateGuid();
        _securityEvent = new AuthSecurityEventDto
        {
            AuthSecurityEventId = _eventId,
            OccurredAt = CreateTimestamp(),
            EventType = AuthSecurityEventTypes.LoginFailed,
            Success = false,
            FailureReason = "Unknown user",
        };
    }

    protected override GetAuthSecurityEventUseCase BuildSubject() =>
        new(ActorTracker.Object, Repository.Object, new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenAdmin_ReturnsForbidden()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);

        var response = await BuildSubject().Execute(Request(_eventId));

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(TenantErrorCodes.ActorNotSuperAdmin, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenIdEmpty_ReturnsInvalidRequest()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);

        var response = await BuildSubject().Execute(Request(Guid.Empty));

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(AuthSecurityEventErrorCodes.EventIdInvalid, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenMissing_ReturnsNotFound()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        Repository
            .Setup(r => r.GetByIdAsync(_eventId, _ct))
            .ReturnsAsync((AuthSecurityEventDto?)null);

        var response = await BuildSubject().Execute(Request(_eventId));

        Assert.Equal(UseCaseStatus.NotFound, response.Status);
        Assert.Equal(AuthSecurityEventErrorCodes.EventNotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_ReturnsEvent()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        Repository.Setup(r => r.GetByIdAsync(_eventId, _ct)).ReturnsAsync(_securityEvent);

        var response = await BuildSubject().Execute(Request(_eventId));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(_eventId, response.Result!.AuthSecurityEventId);
    }

    private ProcessRequest<GetAuthSecurityEventRequest> Request(Guid eventId) =>
        ProcessRequest<GetAuthSecurityEventRequest>.From(
            new GetAuthSecurityEventRequest { AuthSecurityEventId = eventId },
            _ct);
}
