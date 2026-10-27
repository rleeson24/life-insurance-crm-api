using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Services;
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
            new TenantInputValidator(),
            new SecurityAudit(NullAuthSecurityEventRecorder.Instance));

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
            .Setup(r => r.InsertAsync(
                "North Agency",
                It.IsAny<TenantContactFields>(),
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_createdTenant);

        var response = await BuildSubject().Execute(ProcessRequest<CreateTenantModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(_createdTenant.TenantId, response.Result!.TenantId);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_RecordsCreatedOrganizationWithoutName()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        TenantRepository
            .Setup(r => r.InsertAsync(
                "North Agency",
                It.IsAny<TenantContactFields>(),
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_createdTenant);
        var recorder = new Mock<IAuthSecurityEventRecorder>();
        recorder
            .Setup(r => r.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var subject = new CreateTenantUseCase(
            ActorTracker.Object,
            NowProvider.Object,
            TenantRepository.Object,
            new ClientUseCaseHelpers(),
            new TenantInputValidator(),
            new SecurityAudit(recorder.Object));

        var response = await subject.Execute(ProcessRequest<CreateTenantModel>.From(_inputModel, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        recorder.Verify(
            r => r.RecordAsync(
                AuthSecurityEventTypes.TenantChanged,
                true,
                null,
                "tenants",
                _ct,
                SecurityAudit.StatusCreated,
                null,
                _createdTenant.TenantId,
                "created;isActive=true"),
            Times.Once);
        recorder.Verify(
            r => r.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>(),
                It.Is<string?>(detail => detail != null && detail.Contains("North Agency"))),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WhenSuperAdmin_StoresTrimmedContactDetails()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        var model = new CreateTenantModel
        {
            Name = "North Agency",
            AddressLine1 = " 1 Main St ",
            AddressLine2 = " ",
            City = " Boston ",
            State = "ma",
            PostalCode = " 02110 ",
            NoticesEmail = " notices@example.com ",
            IncidentContact = " Ada Lovelace ",
            IncidentTelephone = " 555-0100 ",
        };
        TenantRepository
            .Setup(r => r.InsertAsync(
                "North Agency",
                It.Is<TenantContactFields>(contact =>
                    contact.AddressLine1 == "1 Main St"
                    && contact.AddressLine2 == null
                    && contact.City == "Boston"
                    && contact.State == "MA"
                    && contact.PostalCode == "02110"
                    && contact.NoticesEmail == "notices@example.com"
                    && contact.IncidentContact == "Ada Lovelace"
                    && contact.IncidentTelephone == "555-0100"),
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_createdTenant);

        var response = await BuildSubject().Execute(ProcessRequest<CreateTenantModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }
}
