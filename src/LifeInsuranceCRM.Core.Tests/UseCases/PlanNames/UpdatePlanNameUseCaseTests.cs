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

public class UpdatePlanNameUseCaseTests : UseCaseTestBase<UpdatePlanNameUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _planNameId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly UpdatePlanNameModel _inputModel;
    private readonly PlanName _existing;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IPlanNameRepository> PlanNameRepository => MockFor<IPlanNameRepository>();

    public UpdatePlanNameUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _planNameId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = new UpdatePlanNameModel
        {
            Kind = PlanNameKind.Secondary,
            PlanNameId = _planNameId,
            Name = "AARP",
        };
        _existing = new PlanName
        {
            PlanNameId = _planNameId,
            TenantId = _tenantId,
            PlanYear = 2026,
            Name = "Old Name",
            CreatedAt = _now,
            CreatedByUserId = _userId,
            UpdatedAt = _now,
            UpdatedByUserId = _userId,
        };
    }

    protected override UpdatePlanNameUseCase BuildSubject() =>
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

        var response = await BuildSubject().Execute(ProcessRequest<UpdatePlanNameModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(PlanNameErrorCodes.ActorNotAdmin, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenMissing_ReturnsNotFound()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        PlanNameRepository
            .Setup(r => r.GetByIdAsync(PlanNameKind.Secondary, _planNameId, _ct))
            .ReturnsAsync((PlanName?)null);

        var response = await BuildSubject().Execute(ProcessRequest<UpdatePlanNameModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.NotFound, response.Status);
        Assert.Equal(PlanNameErrorCodes.NotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenValid_UpdatesName()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        PlanNameRepository
            .Setup(r => r.GetByIdAsync(PlanNameKind.Secondary, _planNameId, _ct))
            .ReturnsAsync(_existing);
        PlanNameRepository
            .Setup(r => r.ExistsByNameAsync(
                PlanNameKind.Secondary,
                (short)2026,
                "AARP",
                _planNameId,
                _ct))
            .ReturnsAsync(false);
        PlanNameRepository
            .Setup(r => r.UpdateNameAsync(
                PlanNameKind.Secondary,
                _planNameId,
                "AARP",
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(new PlanName
            {
                PlanNameId = _planNameId,
                TenantId = _tenantId,
                PlanYear = 2026,
                Name = "AARP",
                CreatedAt = _now,
                CreatedByUserId = _userId,
                UpdatedAt = _now,
                UpdatedByUserId = _userId,
            });

        var response = await BuildSubject().Execute(ProcessRequest<UpdatePlanNameModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal("AARP", response.Result!.Name);
    }
}
