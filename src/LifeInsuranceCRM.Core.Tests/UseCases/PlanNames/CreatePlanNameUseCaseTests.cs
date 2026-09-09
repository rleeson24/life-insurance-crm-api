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

public class CreatePlanNameUseCaseTests : UseCaseTestBase<CreatePlanNameUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly CreatePlanNameModel _inputModel;
    private readonly PlanName _created;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IPlanNameRepository> PlanNameRepository => MockFor<IPlanNameRepository>();

    public CreatePlanNameUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = new CreatePlanNameModel
        {
            Kind = PlanNameKind.Medicare,
            PlanYear = 2026,
            Name = " Humana Gold Plus ",
        };
        _created = new PlanName
        {
            PlanNameId = CreateGuid(),
            TenantId = _tenantId,
            PlanYear = 2026,
            Name = "Humana Gold Plus",
            CreatedAt = _now,
            CreatedByUserId = _userId,
            UpdatedAt = _now,
            UpdatedByUserId = _userId,
        };
    }

    protected override CreatePlanNameUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            PlanNameRepository.Object,
            new PlanNameMapper(),
            new ClientUseCaseHelpers(),
            new PlanNameInputValidator());

    [Fact]
    public async Task Execute_WhenUnauthenticated_ReturnsUnauthorized()
    {
        ActorTracker.SetupUnauthenticatedActor();

        var response = await BuildSubject().Execute(ProcessRequest<CreatePlanNameModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Assert.Equal(PlanNameErrorCodes.ActorNotAuthenticated, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenDuplicate_ReturnsConflict()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);
        PlanNameRepository
            .Setup(r => r.ExistsByNameAsync(PlanNameKind.Medicare, (short)2026, "Humana Gold Plus", null, _ct))
            .ReturnsAsync(true);

        var response = await BuildSubject().Execute(ProcessRequest<CreatePlanNameModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Conflict, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameAlreadyExists, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenValid_InsertsTrimmedName()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        PlanNameRepository
            .Setup(r => r.ExistsByNameAsync(PlanNameKind.Medicare, (short)2026, "Humana Gold Plus", null, _ct))
            .ReturnsAsync(false);
        PlanNameRepository
            .Setup(r => r.InsertAsync(
                PlanNameKind.Medicare,
                _tenantId,
                (short)2026,
                "Humana Gold Plus",
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_created);

        var response = await BuildSubject().Execute(ProcessRequest<CreatePlanNameModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(_created.PlanNameId, response.Result!.PlanNameId);
        Assert.Equal("Humana Gold Plus", response.Result.Name);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear)]
    [InlineData(PlanNameInputValidator.MaxPlanYear)]
    public async Task Execute_WhenYearAtBounds_Inserts(short planYear)
    {
        var model = _inputModel with { PlanYear = planYear };
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        PlanNameRepository
            .Setup(r => r.ExistsByNameAsync(PlanNameKind.Medicare, planYear, "Humana Gold Plus", null, _ct))
            .ReturnsAsync(false);
        PlanNameRepository
            .Setup(r => r.InsertAsync(
                PlanNameKind.Medicare,
                _tenantId,
                planYear,
                "Humana Gold Plus",
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_created);

        var response = await BuildSubject().Execute(ProcessRequest<CreatePlanNameModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear - 1)]
    [InlineData(PlanNameInputValidator.MaxPlanYear + 1)]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    public async Task Execute_WhenYearOutsideBounds_ReturnsInvalidRequest(short planYear)
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);
        var model = _inputModel with { PlanYear = planYear };

        var response = await BuildSubject().Execute(ProcessRequest<CreatePlanNameModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.PlanYearInvalid, response.ErrorCode);
        PlanNameRepository.Verify(
            r => r.InsertAsync(
                It.IsAny<PlanNameKind>(),
                It.IsAny<Guid>(),
                It.IsAny<short>(),
                It.IsAny<string>(),
                It.IsAny<AuditStamp>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
