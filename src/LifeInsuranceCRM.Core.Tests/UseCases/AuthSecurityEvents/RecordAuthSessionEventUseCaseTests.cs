using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.AuthSecurityEvents;

public class RecordAuthSessionEventUseCaseTests : UseCaseTestBase<RecordAuthSessionEventUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IAuthSecurityEventRecorder> Recorder => MockFor<IAuthSecurityEventRecorder>();

    public RecordAuthSessionEventUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
    }

    protected override RecordAuthSessionEventUseCase BuildSubject() =>
        new(ActorTracker.Object, Recorder.Object, new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenLoginSucceeded_RecordsEvent()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);

        var response = await BuildSubject().Execute(Request(AuthSecurityEventTypes.LoginSucceeded));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Recorder.Verify(
            r => r.RecordAsync(
                AuthSecurityEventTypes.LoginSucceeded,
                true,
                null,
                null,
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenLogoutWithoutActor_ReturnsUnauthorized()
    {
        var response = await BuildSubject().Execute(Request(AuthSecurityEventTypes.Logout));

        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Recorder.Verify(
            r => r.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WhenLoginFailed_RecordsSanitizedReasonWithoutActor()
    {
        var response = await BuildSubject().Execute(
            Request(AuthSecurityEventTypes.LoginFailed, "access_denied"));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Recorder.Verify(
            r => r.RecordAsync(
                AuthSecurityEventTypes.LoginFailed,
                false,
                "access_denied",
                null,
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenLoginFailedReasonIsFreeText_RecordsGenericReason()
    {
        var response = await BuildSubject().Execute(
            Request(AuthSecurityEventTypes.LoginFailed, "user@contoso.com failed"));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Recorder.Verify(
            r => r.RecordAsync(
                AuthSecurityEventTypes.LoginFailed,
                false,
                "login_failed",
                null,
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenEventTypeUnknown_ReturnsInvalidRequest()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);

        var response = await BuildSubject().Execute(Request(AuthSecurityEventTypes.TenantResolved));

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(AuthSecurityEventErrorCodes.EventTypeInvalid, response.ErrorCode);
    }

    private ProcessRequest<RecordAuthSessionEventRequest> Request(string eventType, string? failureReason = null) =>
        ProcessRequest<RecordAuthSessionEventRequest>.From(
            new RecordAuthSessionEventRequest
            {
                EventType = eventType,
                FailureReason = failureReason,
            },
            _ct);
}
