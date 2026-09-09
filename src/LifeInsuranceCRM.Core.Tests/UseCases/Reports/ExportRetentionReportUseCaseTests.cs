using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Reports;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Reports;

public class ExportRetentionReportUseCaseTests : UseCaseTestBase<ExportRetentionReportUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetRetentionReportRequest _request = new();
    private readonly RetentionReportDto _report;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IGetRetentionReportUseCase> GetRetentionReportUseCase => MockFor<IGetRetentionReportUseCase>();
    private Mock<IAuthSecurityEventRecorder> AuthSecurityEventRecorder => MockFor<IAuthSecurityEventRecorder>();

    public ExportRetentionReportUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _report = Create<RetentionReportDto>();
    }

    protected override ExportRetentionReportUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            GetRetentionReportUseCase.Object,
            new ReportUseCaseHelpers(new ClientUseCaseHelpers()),
            AuthSecurityEventRecorder.Object);

    public sealed class Success_Setup : ExportRetentionReportUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Admin);
            GetRetentionReportUseCase
                .Setup(u => u.Execute(It.Is<ProcessRequest<GetRetentionReportRequest>>(
                    r => r.Payload == _request && r.CancellationToken == _ct)))
                .ReturnsAsync(ProcessResponse<RetentionReportDto>.Succeeded(_report));
            AuthSecurityEventRecorder
                .Setup(r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    true,
                    null,
                    "retention",
                    _ct))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetRetentionReportRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<RetentionReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Records_ReportExported_WithoutPhi()
        {
            _fixture.AuthSecurityEventRecorder.Verify(
                r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    true,
                    null,
                    "retention",
                    _fixture._ct),
                Times.Once);
        }
    }
}
