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
        OrganizationUserRepository.Verify(
            r => r.InsertTenantAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AuditStamp>(), _ct),
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
    public async Task Execute_WhenCreateNewTenant_InsertsTenantThenUser()
    {
        var model = _inputModel with { CreateNewTenant = true, NewTenantName = "North Agency" };
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        OrganizationUserRepository
            .Setup(r => r.ExistsByUserIdAsync(model.UserId, _ct))
            .ReturnsAsync(false);
        OrganizationUserRepository
            .Setup(r => r.InsertAsync(
                It.IsAny<Guid>(),
                model.UserId,
                model.EmailAddress,
                model.DisplayName,
                model.Role,
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(new OrganizationUserDto
            {
                OrganizationUserId = _createdUser.OrganizationUserId,
                TenantId = _createdUser.TenantId,
                TenantName = "North Agency",
                UserId = _createdUser.UserId,
                EmailAddress = _createdUser.EmailAddress,
                DisplayName = _createdUser.DisplayName,
                Role = _createdUser.Role,
                IsActive = true,
                CreatedAt = _createdUser.CreatedAt,
                UpdatedAt = _createdUser.UpdatedAt,
            });

        var response = await BuildSubject().Execute(ProcessRequest<CreateOrganizationUserModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        OrganizationUserRepository.Verify(
            r => r.InsertTenantAsync(It.IsAny<Guid>(), "North Agency", It.IsAny<AuditStamp>(), _ct),
            Times.Once);
    }
}
