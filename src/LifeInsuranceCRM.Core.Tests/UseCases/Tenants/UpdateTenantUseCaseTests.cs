using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Tenants;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Tenants;

public class UpdateTenantUseCaseTests : UseCaseTestBase<UpdateTenantUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly TenantDto _existing;
    private readonly UpdateTenantModel _updateModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<ITenantRepository> TenantRepository => MockFor<ITenantRepository>();

    public UpdateTenantUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _existing = new TenantDto
        {
            TenantId = CreateGuid(),
            Name = "North Agency",
            IsActive = true,
            CreatedAt = _now,
            UpdatedAt = _now,
        };
        _updateModel = new UpdateTenantModel
        {
            TenantId = _existing.TenantId,
            IsActive = false,
        };
    }

    protected override UpdateTenantUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            TenantRepository.Object,
            new ClientUseCaseHelpers(),
            new TenantInputValidator());

    [Fact]
    public async Task Execute_WhenMissing_ReturnsNotFound()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        TenantRepository
            .Setup(r => r.GetByIdAsync(_existing.TenantId, _ct))
            .ReturnsAsync((TenantDto?)null);

        var response = await BuildSubject().Execute(ProcessRequest<UpdateTenantModel>.From(_updateModel, _ct));

        Assert.Equal(UseCaseStatus.NotFound, response.Status);
        Assert.Equal(TenantErrorCodes.TenantNotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_DeactivatesTenant()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        TenantRepository
            .Setup(r => r.GetByIdAsync(_existing.TenantId, _ct))
            .ReturnsAsync(_existing);
        TenantRepository
            .Setup(r => r.UpdateAsync(_existing.TenantId, null, false, It.IsAny<AuditStamp>(), _ct))
            .ReturnsAsync(new TenantDto
            {
                TenantId = _existing.TenantId,
                Name = _existing.Name,
                IsActive = false,
                CreatedAt = _existing.CreatedAt,
                UpdatedAt = _now,
            });

        var response = await BuildSubject().Execute(ProcessRequest<UpdateTenantModel>.From(_updateModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.False(response.Result!.IsActive);
    }
}
