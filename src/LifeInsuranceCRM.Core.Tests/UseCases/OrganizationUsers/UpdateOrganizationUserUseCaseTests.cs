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

public class UpdateOrganizationUserUseCaseTests : UseCaseTestBase<UpdateOrganizationUserUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly OrganizationUserDto _existingAdmin;
    private readonly UpdateOrganizationUserModel _updateModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IOrganizationUserRepository> OrganizationUserRepository => MockFor<IOrganizationUserRepository>();

    public UpdateOrganizationUserUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _existingAdmin = new OrganizationUserDto
        {
            OrganizationUserId = CreateGuid(),
            TenantId = _tenantId,
            UserId = CreateGuid(),
            DisplayName = "Admin",
            EmailAddress = "admin@example.com",
            Role = OrganizationRoles.Admin,
            IsActive = true,
            CreatedAt = _now,
            UpdatedAt = _now,
        };
        _updateModel = new UpdateOrganizationUserModel
        {
            OrganizationUserId = _existingAdmin.OrganizationUserId,
            DisplayName = "Admin",
            EmailAddress = "admin@example.com",
            Role = OrganizationRoles.Agent,
            IsActive = true,
        };
    }

    protected override UpdateOrganizationUserUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            OrganizationUserRepository.Object,
            new ClientUseCaseHelpers(),
            new OrganizationUserInputValidator());

    [Fact]
    public async Task Execute_WhenDemotingLastAdmin_ReturnsInvalidRequest()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        OrganizationUserRepository
            .Setup(r => r.GetByOrganizationUserIdAsync(_existingAdmin.OrganizationUserId, _ct))
            .ReturnsAsync(_existingAdmin);
        OrganizationUserRepository
            .Setup(r => r.CountActiveAdminsInTenantAsync(_tenantId, _ct))
            .ReturnsAsync(1);

        var response = await BuildSubject().Execute(
            ProcessRequest<UpdateOrganizationUserModel>.From(_updateModel, _ct));

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.LastAdmin, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenUserInOtherTenant_ReturnsNotFound()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        OrganizationUserRepository
            .Setup(r => r.GetByOrganizationUserIdAsync(_existingAdmin.OrganizationUserId, _ct))
            .ReturnsAsync(new OrganizationUserDto
            {
                OrganizationUserId = _existingAdmin.OrganizationUserId,
                TenantId = CreateGuid(),
                UserId = _existingAdmin.UserId,
                DisplayName = _existingAdmin.DisplayName,
                EmailAddress = _existingAdmin.EmailAddress,
                Role = _existingAdmin.Role,
                IsActive = _existingAdmin.IsActive,
                CreatedAt = _existingAdmin.CreatedAt,
                UpdatedAt = _existingAdmin.UpdatedAt,
            });

        var response = await BuildSubject().Execute(
            ProcessRequest<UpdateOrganizationUserModel>.From(_updateModel, _ct));

        Assert.Equal(UseCaseStatus.NotFound, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.UserNotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenValid_UpdatesUser()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        OrganizationUserRepository
            .Setup(r => r.GetByOrganizationUserIdAsync(_existingAdmin.OrganizationUserId, _ct))
            .ReturnsAsync(_existingAdmin);
        OrganizationUserRepository
            .Setup(r => r.CountActiveAdminsInTenantAsync(_tenantId, _ct))
            .ReturnsAsync(2);
        OrganizationUserRepository
            .Setup(r => r.UpdateAsync(
                _existingAdmin.OrganizationUserId,
                _updateModel.EmailAddress,
                _updateModel.DisplayName,
                OrganizationRoles.Agent,
                true,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(new OrganizationUserDto
            {
                OrganizationUserId = _existingAdmin.OrganizationUserId,
                TenantId = _existingAdmin.TenantId,
                TenantName = _existingAdmin.TenantName,
                UserId = _existingAdmin.UserId,
                DisplayName = _existingAdmin.DisplayName,
                EmailAddress = _existingAdmin.EmailAddress,
                Role = OrganizationRoles.Agent,
                IsActive = true,
                CreatedAt = _existingAdmin.CreatedAt,
                UpdatedAt = _existingAdmin.UpdatedAt,
            });

        var response = await BuildSubject().Execute(
            ProcessRequest<UpdateOrganizationUserModel>.From(_updateModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(OrganizationRoles.Agent, response.Result!.Role);
    }
}
