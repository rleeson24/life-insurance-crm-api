using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Reports;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Reports;

public class ReportUseCaseHelpersTests
{
    private readonly ReportUseCaseHelpers _subject = new(new ClientUseCaseHelpers());
    private readonly Mock<IActorTracker> _actorTracker = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(ReportConstants.MinPlanYear)]
    [InlineData(ReportConstants.MinPlanYear + 1)]
    [InlineData(ReportConstants.MaxPlanYear - 1)]
    [InlineData(ReportConstants.MaxPlanYear)]
    public void ValidatePlanYear_WhenAtOrJustInsideBounds_ReturnsSuccess(short planYear)
    {
        var response = _subject.ValidatePlanYear(planYear);

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(ReportConstants.MinPlanYear - 1)]
    [InlineData(ReportConstants.MaxPlanYear + 1)]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidatePlanYear_WhenOutsideBounds_ReturnsInvalidRequest(short planYear)
    {
        var response = _subject.ValidatePlanYear(planYear);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ReportErrorCodes.PlanYearInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateViewer_WhenUnauthenticated_ReturnsUnauthorized()
    {
        _actorTracker.SetupUnauthenticatedActor();

        var response = _subject.ValidateViewer(_actorTracker.Object);

        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Assert.Equal(ReportErrorCodes.ActorNotAuthenticated, response.ErrorCode);
    }

    [Fact]
    public void ValidateExporter_WhenAgent_ReturnsForbidden()
    {
        _actorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);

        var response = _subject.ValidateExporter(_actorTracker.Object);

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(ReportErrorCodes.ActorNotAdmin, response.ErrorCode);
    }

    [Fact]
    public void ValidateExporter_WhenAdmin_ReturnsSuccess()
    {
        _actorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Admin);

        var response = _subject.ValidateExporter(_actorTracker.Object);

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }
}
