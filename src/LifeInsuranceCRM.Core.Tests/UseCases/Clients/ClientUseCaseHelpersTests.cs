using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class ClientUseCaseHelpersTests
{
    private readonly ClientUseCaseHelpers _subject = new();
    private readonly Mock<IActorTracker> _actorTracker = new();
    private readonly Mock<INowProvider> _nowProvider = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    public ClientUseCaseHelpersTests()
    {
        _actorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        _nowProvider.Setup(n => n.UtcNow).Returns(_now);
    }

    [Fact]
    public void ValidateActor_WhenAuthenticated_ReturnsSuccess()
    {
        var response = _subject.ValidateActor(_actorTracker.Object);
        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateActor_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _actorTracker.SetupUnauthenticatedActor();
        var response = _subject.ValidateActor(_actorTracker.Object);
        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Assert.Equal(ClientErrorCodes.ActorNotAuthenticated, response.ErrorCode);
    }

    [Fact]
    public void ValidateClientId_WhenEmpty_ReturnsInvalidRequest()
    {
        var response = _subject.ValidateClientId(Guid.Empty);
        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.ClientIdInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateClientAccess_WhenClientIdEmpty_ReturnsInvalidRequest()
    {
        var response = _subject.ValidateClientAccess(_actorTracker.Object, Guid.Empty);
        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.ClientIdInvalid, response.ErrorCode);
    }

    [Fact]
    public void CreateAuditStamp_ReturnsExpectedValues()
    {
        var stamp = _subject.CreateAuditStamp(_actorTracker.Object, _nowProvider.Object);
        Assert.Equal(_userId, stamp.UserId);
        Assert.Equal(_now, stamp.Timestamp);
    }
}
