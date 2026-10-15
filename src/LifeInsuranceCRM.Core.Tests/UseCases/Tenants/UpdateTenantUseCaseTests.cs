using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Services;
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
            AddressLine1 = "1 Main St",
            City = "Boston",
            State = "MA",
            PostalCode = "02110",
            NoticesEmail = "notices@example.com",
            IncidentContact = "Ada Lovelace",
            IncidentTelephone = "555-0100",
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
            new TenantInputValidator(),
            new SecurityAudit(NullAuthSecurityEventRecorder.Instance));

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
            .Setup(r => r.UpdateAsync(
                _existing.TenantId,
                null,
                false,
                It.Is<TenantContactFields>(contact =>
                    contact.AddressLine1 == _existing.AddressLine1
                    && contact.NoticesEmail == _existing.NoticesEmail
                    && contact.IncidentContact == _existing.IncidentContact
                    && contact.IncidentTelephone == _existing.IncidentTelephone),
                It.IsAny<AuditStamp>(),
                _ct))
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

    [Fact]
    public async Task Execute_WhenContactChanges_RecordsFieldNamesWithoutValues()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.SuperAdmin);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        var model = new UpdateTenantModel
        {
            TenantId = _existing.TenantId,
            NoticesEmail = " ops@example.com ",
            IncidentTelephone = "",
        };
        TenantRepository
            .Setup(r => r.GetByIdAsync(_existing.TenantId, _ct))
            .ReturnsAsync(_existing);
        TenantRepository
            .Setup(r => r.UpdateAsync(
                _existing.TenantId,
                null,
                null,
                It.Is<TenantContactFields>(contact =>
                    contact.NoticesEmail == "ops@example.com"
                    && contact.IncidentTelephone == null
                    && contact.AddressLine1 == _existing.AddressLine1
                    && contact.IncidentContact == _existing.IncidentContact),
                It.IsAny<AuditStamp>(),
                _ct))
            .ReturnsAsync(_existing);
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
        var subject = new UpdateTenantUseCase(
            ActorTracker.Object,
            NowProvider.Object,
            TenantRepository.Object,
            new ClientUseCaseHelpers(),
            new TenantInputValidator(),
            new SecurityAudit(recorder.Object));

        var response = await subject.Execute(ProcessRequest<UpdateTenantModel>.From(model, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        recorder.Verify(
            r => r.RecordAsync(
                AuthSecurityEventTypes.TenantChanged,
                true,
                null,
                "tenants",
                _ct,
                SecurityAudit.StatusOk,
                null,
                _existing.TenantId,
                "fields=noticesEmail,incidentTelephone"),
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
                It.Is<string?>(detail =>
                    detail != null
                    && (detail.Contains("ops@example.com") || detail.Contains("555-0100")))),
            Times.Never);
    }
}
