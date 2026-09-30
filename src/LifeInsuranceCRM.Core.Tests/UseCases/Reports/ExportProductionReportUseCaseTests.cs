using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Reports;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Reports;

public class ExportProductionReportUseCaseTests : UseCaseTestBase<ExportProductionReportUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetProductionReportRequest _request;
    private readonly ProductionReportDto _report;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IGetProductionReportUseCase> GetProductionReportUseCase => MockFor<IGetProductionReportUseCase>();
    private Mock<IAuthSecurityEventRecorder> AuthSecurityEventRecorder => MockFor<IAuthSecurityEventRecorder>();

    public ExportProductionReportUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _request = new GetProductionReportRequest { PlanYear = 2026 };
        _report = Create<ProductionReportDto>();
    }

    protected override ExportProductionReportUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            GetProductionReportUseCase.Object,
            new ReportUseCaseHelpers(new ClientUseCaseHelpers()),
            new SecurityAudit(AuthSecurityEventRecorder.Object));

    public sealed class Success_Setup : ExportProductionReportUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Admin);
            GetProductionReportUseCase
                .Setup(u => u.Execute(It.Is<ProcessRequest<GetProductionReportRequest>>(
                    r => r.Payload == _request && r.CancellationToken == _ct),
                    false))
                .ReturnsAsync(ProcessResponse<ProductionReportDto>.Succeeded(_report));
            AuthSecurityEventRecorder
                .Setup(r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    true,
                    null,
                    "production:2026",
                    _ct,
                    200,
                    _report.Medicare.Count + _report.Drug.Count + _report.Secondary.Count,
                    null,
                    "planYear=2026"))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetProductionReportRequest>.From(_request, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class Success : IClassFixture<Success_Setup>
    {
        private readonly Success_Setup _fixture;

        public Success(Success_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsSuccess()
        {
            var response = (ProcessResponse<ProductionReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Records_ReportExported_WithPlanYearResource()
        {
            _fixture.AuthSecurityEventRecorder.Verify(
                r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    true,
                    null,
                    "production:2026",
                    _fixture._ct,
                    200,
                    _fixture._report.Medicare.Count + _fixture._report.Drug.Count + _fixture._report.Secondary.Count,
                    null,
                    "planYear=2026"),
                Times.Once);
        }
    }

    public sealed class InvalidPlanYear_Setup : ExportProductionReportUseCaseTests, IAsyncLifetime
    {
        private readonly GetProductionReportRequest _invalidRequest =
            new() { PlanYear = 1800 };

        public InvalidPlanYear_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Admin);
            GetProductionReportUseCase
                .Setup(u => u.Execute(It.Is<ProcessRequest<GetProductionReportRequest>>(
                    r => r.Payload.PlanYear == 1800 && r.CancellationToken == _ct),
                    false))
                .ReturnsAsync(ProcessResponse<ProductionReportDto>.InvalidRequestResponse(
                    "Plan year must be between 1990 and 2100",
                    ReportErrorCodes.PlanYearInvalid));
            AuthSecurityEventRecorder
                .Setup(r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    false,
                    "Invalid plan year",
                    "production:1800",
                    _ct,
                    400,
                    null,
                    null,
                    "planYear=1800"))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetProductionReportRequest>.From(_invalidRequest, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class InvalidPlanYear : IClassFixture<InvalidPlanYear_Setup>
    {
        private readonly InvalidPlanYear_Setup _fixture;

        public InvalidPlanYear(InvalidPlanYear_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsInvalidRequest()
        {
            var response = (ProcessResponse<ProductionReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        }

        [Fact]
        public void Records_FailedExport_WithoutPhi()
        {
            _fixture.AuthSecurityEventRecorder.Verify(
                r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    false,
                    "Invalid plan year",
                    "production:1800",
                    _fixture._ct,
                    400,
                    null,
                    null,
                    "planYear=1800"),
                Times.Once);
        }
    }
}
