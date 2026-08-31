using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Import;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Imports;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Imports;

public class ImportAccessDatabaseUseCaseTests : UseCaseTestBase<ImportAccessDatabaseUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly AccessImportModel _payload = new()
    {
        Clients = [],
    };

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IAccessImportMapper> Mapper => MockFor<IAccessImportMapper>();
    private Mock<IAccessImportRepository> Repository => MockFor<IAccessImportRepository>();

    public ImportAccessDatabaseUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
    }

    protected override ImportAccessDatabaseUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            Mapper.Object,
            Repository.Object,
            new ClientUseCaseHelpers());

    [Fact]
    public async Task Execute_WhenUnauthenticated_ReturnsUnauthorized()
    {
        ActorTracker.SetupUnauthenticatedActor();

        var response = await BuildSubject().Execute(ProcessRequest<AccessImportModel>.From(_payload, _ct));

        Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        Assert.Equal(ImportErrorCodes.ActorNotAuthenticated, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenAgent_ReturnsForbidden()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Agent);

        var response = await BuildSubject().Execute(ProcessRequest<AccessImportModel>.From(_payload, _ct));

        Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        Assert.Equal(ImportErrorCodes.ActorNotAdmin, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenNoMappedClients_ReturnsInvalidRequest()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        Mapper.Setup(m => m.Map(_payload, _now)).Returns(new MappedAccessImport());

        var response = await BuildSubject().Execute(ProcessRequest<AccessImportModel>.From(_payload, _ct));

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ImportErrorCodes.NoClients, response.ErrorCode);
        Repository.Verify(
            r => r.ImportAsync(It.IsAny<MappedAccessImport>(), It.IsAny<Guid>(), It.IsAny<AuditStamp>(), _ct),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WhenTenantAlreadyHasClients_ReturnsConflict()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        var mapped = CreateMappedImport();
        Mapper.Setup(m => m.Map(_payload, _now)).Returns(mapped);
        Repository
            .Setup(r => r.ImportAsync(mapped, _tenantId, It.IsAny<AuditStamp>(), _ct))
            .ReturnsAsync(new AccessImportPersistResult { TenantAlreadyHasClients = true });

        var response = await BuildSubject().Execute(ProcessRequest<AccessImportModel>.From(_payload, _ct));

        Assert.Equal(UseCaseStatus.Conflict, response.Status);
        Assert.Equal(ImportErrorCodes.TenantNotEmpty, response.ErrorCode);
    }

    [Fact]
    public async Task Execute_WhenAdminAndEmptyTenant_InsertsMappedRows()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        NowProvider.Setup(n => n.UtcNow).Returns(_now);
        var mapped = CreateMappedImport();
        mapped = new MappedAccessImport
        {
            Clients = mapped.Clients,
            MajorMedicalEnrollments = mapped.MajorMedicalEnrollments,
            DrugPlanEnrollments = mapped.DrugPlanEnrollments,
            SecondaryEnrollments = mapped.SecondaryEnrollments,
            Interactions = mapped.Interactions,
            PlanNames = mapped.PlanNames,
            Warnings = ["Skipped Medicare enrollment for unknown client 99."],
        };
        Mapper.Setup(m => m.Map(_payload, _now)).Returns(mapped);
        Repository
            .Setup(r => r.ImportAsync(mapped, _tenantId, It.IsAny<AuditStamp>(), _ct))
            .ReturnsAsync(new AccessImportPersistResult
            {
                MedicarePlanNamesInserted = 1,
                DrugPlanNamesInserted = 1,
                SecondaryPlanNamesInserted = 0,
            });

        var response = await BuildSubject().Execute(ProcessRequest<AccessImportModel>.From(_payload, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(1, response.Result!.ClientsInserted);
        Assert.Equal(1, response.Result.MajorMedicalEnrollmentsInserted);
        Assert.Equal(1, response.Result.DrugPlanEnrollmentsInserted);
        Assert.Equal(1, response.Result.InteractionsInserted);
        Assert.Equal(1, response.Result.MedicarePlanNamesInserted);
        Assert.Equal("Skipped Medicare enrollment for unknown client 99.", Assert.Single(response.Result.Warnings));
    }

    private MappedAccessImport CreateMappedImport()
    {
        var clientId = CreateGuid();
        return new MappedAccessImport
        {
            Clients =
            [
                new MappedImportClient
                {
                    AccessClientId = 1,
                    ClientId = clientId,
                    FirstName = "Pat",
                    LastName = "Kim",
                    IsActive = true,
                },
            ],
            MajorMedicalEnrollments =
            [
                new MappedImportMajorMedicalEnrollment
                {
                    MajorMedicalEnrollmentId = CreateGuid(),
                    ClientId = clientId,
                    RecordedAt = _now,
                    PlanName = "Humana",
                },
            ],
            DrugPlanEnrollments =
            [
                new MappedImportDrugPlanEnrollment
                {
                    DrugPlanEnrollmentId = CreateGuid(),
                    ClientId = clientId,
                    RecordedAt = _now,
                    PlanName = "SilverScript",
                },
            ],
            Interactions =
            [
                new MappedImportInteraction
                {
                    ClientInteractionId = CreateGuid(),
                    ClientId = clientId,
                    ContactedAt = _now,
                    Summary = "Called",
                },
            ],
            PlanNames =
            [
                new MappedImportPlanName { Kind = PlanNameKind.Medicare, PlanYear = 2026, Name = "Humana" },
            ],
        };
    }
}
