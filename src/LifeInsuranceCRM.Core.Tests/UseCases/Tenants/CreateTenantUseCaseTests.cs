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

public class CreateTenantUseCaseTests : UseCaseTestBase<CreateTenantUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly CreateTenantModel _inputModel;
    private readonly TenantDto _createdTenant;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<ITenantRepository> TenantRepository => MockFor<ITenantRepository>();

    public CreateTenantUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = new CreateTenantModel { Name = "North Agency" };
        _createdTenant = new TenantDto
        {
            TenantId = CreateGuid(),
            Name = "North Agency",
            IsActive = true,
            CreatedAt = _now,
            UpdatedAt = _now,
        };
    }

    protected override CreateTenantUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            TenantRepository.Object,
            new ClientUseCaseHelpers(),
            new TenantInputValidator());

    [Fact]
    public async Task Execute_WhenAdmin_ReturnsForbidden()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);

        var response = await BuildSubject().Execute(ProcessRequest<CreateTenantModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(TenantErrorCodes.ActorNotSuperAdmin, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_InsertsTenant()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        TenantRepository
            .Setup(r => r.InsertAsync("North Agency", It.IsAny<AuditStamp>(), _ct))
            .ReturnsAsync(_createdTenant);

        var response = await BuildSubject().Execute(ProcessRequest<CreateTenantModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(_createdTenant.TenantId, response.Result!.TenantId);
    }
}
