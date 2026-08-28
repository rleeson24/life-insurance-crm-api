using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.PlanNames;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.PlanNames;

public class DeletePlanNameUseCaseTests : UseCaseTestBase<DeletePlanNameUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _planNameId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly DeletePlanNameRequest _request;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IPlanNameRepository> PlanNameRepository => MockFor<IPlanNameRepository>();

    public DeletePlanNameUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _planNameId = CreateGuid();
        _now = CreateTimestamp();
        _request = new DeletePlanNameRequest
        {
            Kind = PlanNameKind.Medicare,
            PlanNameId = _planNameId,
        };
    }

    protected override DeletePlanNameUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            PlanNameRepository.Object,
            new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenUnauthenticated_ReturnsUnauthorized()
    {
        ActorTracker.SetupUnauthenticatedActor();

        var response = await BuildSubject().Execute(ProcessRequest<DeletePlanNameRequest>.From(_request, _ct));

        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Assert.Equal(PlanNameErrorCodes.ActorNotAuthenticated, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenMissing_ReturnsNotFound()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        PlanNameRepository
            .Setup(r => r.SoftDeleteAsync(
                PlanNameKind.Medicare,
                _planNameId,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(false);

        var response = await BuildSubject().Execute(ProcessRequest<DeletePlanNameRequest>.From(_request, _ct));

        Assert.Equal(UseCaseStatus.NotFound, response.Status);
        Assert.Equal(PlanNameErrorCodes.NotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenValid_SoftDeletes()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        PlanNameRepository
            .Setup(r => r.SoftDeleteAsync(
                PlanNameKind.Medicare,
                _planNameId,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(true);

        var response = await BuildSubject().Execute(ProcessRequest<DeletePlanNameRequest>.From(_request, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.True(response.Result);
    }
}
