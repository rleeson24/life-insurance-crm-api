using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.OrganizationUsers;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.OrganizationUsers;

public class CreateOrganizationUserUseCaseTests : UseCaseTestBase<CreateOrganizationUserUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly CreateOrganizationUserModel _inputModel;
    private readonly OrganizationUserDto _createdUser;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IOrganizationUserRepository> OrganizationUserRepository => MockFor<IOrganizationUserRepository>();
    private Mock<ITenantRepository> TenantRepository => MockFor<ITenantRepository>();

    public CreateOrganizationUserUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = new CreateOrganizationUserModel
        {
            UserId = CreateGuid(),
            EmailAddress = "agent@example.com",
            DisplayName = "New Agent",
            Role = OrganizationRoles.Agent,
        };
        _createdUser = new OrganizationUserDto
        {
            OrganizationUserId = CreateGuid(),
            TenantId = _tenantId,
            TenantName = "Development Tenant",
            UserId = _inputModel.UserId,
            EmailAddress = _inputModel.EmailAddress,
            DisplayName = _inputModel.DisplayName,
            Role = _inputModel.Role,
            IsActive = true,
            CreatedAt = _now,
            UpdatedAt = _now,
        };
    }

    protected override CreateOrganizationUserUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            OrganizationUserRepository.Object,
            TenantRepository.Object,
            new ClientUseCaseHelpers(),
            new OrganizationUserInputValidator());

    [Fact]
    public async Task Execute_WhenValid_InsertsIntoActorTenant()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        OrganizationUserRepository
            .Setup(r => r.ExistsByUserIdAsync(_inputModel.UserId, _ct))
            .ReturnsAsync(false);
        OrganizationUserRepository
            .Setup(r => r.InsertAsync(
                _tenantId,
                _inputModel.UserId,
                _inputModel.EmailAddress,
                _inputModel.DisplayName,
                _inputModel.Role,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_createdUser);

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(_createdUser.OrganizationUserId, response.Result!.OrganizationUserId);
        TenantRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct), Times.Never);
    }

    [Fact]
    public async Task Execute_WhenAdminSuppliesOtherTenantId_IgnoresIt()
    {
        var otherTenantId = CreateGuid();
        var model = _inputModel with { TenantId = otherTenantId };
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        OrganizationUserRepository
            .Setup(r => r.ExistsByUserIdAsync(model.UserId, _ct))
            .ReturnsAsync(false);
        OrganizationUserRepository
            .Setup(r => r.InsertAsync(
                _tenantId,
                model.UserId,
                model.EmailAddress,
                model.DisplayName,
                model.Role,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_createdUser);

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        OrganizationUserRepository.Verify(
            r => r.InsertAsync(
                otherTenantId,
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<AuditStamp>(),
                _ct),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WhenUserExists_ReturnsConflict()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        OrganizationUserRepository
            .Setup(r => r.ExistsByUserIdAsync(_inputModel.UserId, _ct))
            .ReturnsAsync(true);

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Conflict, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.UserAlreadyExists, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenSuperAdminOmitsTenantId_ReturnsInvalidRequest()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.TenantIdRequired, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenSuperAdminTenantMissing_ReturnsNotFound()
    {
        var targetTenantId = CreateGuid();
        var model = _inputModel with { TenantId = targetTenantId };
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        TenantRepository
            .Setup(r => r.GetByIdAsync(targetTenantId, _ct))
            .ReturnsAsync((TenantDto?)null);

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.NotFound, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.TenantNotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_InsertsIntoRequestedTenant()
    {
        var targetTenantId = CreateGuid();
        var model = _inputModel with { TenantId = targetTenantId };
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        TenantRepository
            .Setup(r => r.GetByIdAsync(targetTenantId, _ct))
            .ReturnsAsync(new TenantDto
            {
                TenantId = targetTenantId,
                Name = "North Agency",
                IsActive = true,
                CreatedAt = _now,
                UpdatedAt = _now,
            });
        OrganizationUserRepository
            .Setup(r => r.ExistsByUserIdAsync(model.UserId, _ct))
            .ReturnsAsync(false);
        OrganizationUserRepository
            .Setup(r => r.InsertAsync(
                targetTenantId,
                model.UserId,
                model.EmailAddress,
                model.DisplayName,
                model.Role,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(new OrganizationUserDto
            {
                OrganizationUserId = _createdUser.OrganizationUserId,
                TenantId = targetTenantId,
                TenantName = "North Agency",
                UserId = _createdUser.UserId,
                EmailAddress = _createdUser.EmailAddress,
                DisplayName = _createdUser.DisplayName,
                Role = _createdUser.Role,
                IsActive = _createdUser.IsActive,
                CreatedAt = _createdUser.CreatedAt,
                UpdatedAt = _createdUser.UpdatedAt,
            });

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        OrganizationUserRepository.Verify(
            r => r.InsertAsync(
                targetTenantId,
                model.UserId,
                model.EmailAddress,
                model.DisplayName,
                model.Role,
                It.IsAny<AuditStamp>(),
                _ct),
            Times.Once);
    }
}
