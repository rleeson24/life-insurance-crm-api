using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.PlanNames;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.PlanNames;

public class ClonePlanNamesUseCaseTests : UseCaseTestBase<ClonePlanNamesUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly ClonePlanNamesModel _inputModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IPlanNameRepository> PlanNameRepository => MockFor<IPlanNameRepository>();

    public ClonePlanNamesUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = new ClonePlanNamesModel
        {
            Kind = PlanNameKind.Drug,
            SourceYear = 2025,
            TargetYear = 2026,
        };
    }

    protected override ClonePlanNamesUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            PlanNameRepository.Object,
            new PlanNameMapper(),
            new ClientUseCaseHelpers(),
            new PlanNameInputValidator());

    [Fact]
    public async Task Execute_WhenAgent_ReturnsForbidden()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);

        var response = await BuildSubject().Execute(ProcessRequest<ClonePlanNamesModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(PlanNameErrorCodes.ActorNotAdmin, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenAdmin_ClonesPriorYearAndSkipsDuplicates()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        var cloned = new List<PlanName>
        {
            new()
            {
                PlanNameId = CreateGuid(),
                TenantId = _tenantId,
                PlanYear = 2026,
                Name = "Aetna",
                CreatedAt = _now,
                CreatedByUserId = _userId,
                UpdatedAt = _now,
                UpdatedByUserId = _userId,
            },
        };
        PlanNameRepository
            .Setup(r => r.CountByYearAsync(PlanNameKind.Drug, (short)2025, _ct))
            .ReturnsAsync(3);
        PlanNameRepository
            .Setup(r => r.CloneYearAsync(
                PlanNameKind.Drug,
                _tenantId,
                (short)2025,
                (short)2026,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(cloned);

        var response = await BuildSubject().Execute(ProcessRequest<ClonePlanNamesModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(3, response.Result!.SourceCount);
        Assert.Equal(1, response.Result.ClonedCount);
        Assert.Equal(2, response.Result.SkippedCount);
        Assert.Equal("Aetna", response.Result.Items[0].Name);
    }
}
