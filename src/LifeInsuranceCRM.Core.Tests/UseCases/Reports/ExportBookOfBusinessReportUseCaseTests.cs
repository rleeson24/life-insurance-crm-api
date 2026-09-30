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

public class ExportBookOfBusinessReportUseCaseTests : UseCaseTestBase<ExportBookOfBusinessReportUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetBookOfBusinessReportRequest _request = new();
    private readonly BookOfBusinessReportDto _report;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IGetBookOfBusinessReportUseCase> GetBookOfBusinessReportUseCase =>
        MockFor<IGetBookOfBusinessReportUseCase>();
    private Mock<IAuthSecurityEventRecorder> AuthSecurityEventRecorder => MockFor<IAuthSecurityEventRecorder>();

    public ExportBookOfBusinessReportUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _report = Create<BookOfBusinessReportDto>();
    }

    protected override ExportBookOfBusinessReportUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            GetBookOfBusinessReportUseCase.Object,
            new ReportUseCaseHelpers(new ClientUseCaseHelpers()),
            AuthSecurityEventRecorder.Object);

    public sealed class Success_Setup : ExportBookOfBusinessReportUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, OrganizationRoles.Admin);
            GetBookOfBusinessReportUseCase
                .Setup(u => u.Execute(It.Is<ProcessRequest<GetBookOfBusinessReportRequest>>(
                    r => r.Payload == _request && r.CancellationToken == _ct),
                    false))
                .ReturnsAsync(ProcessResponse<BookOfBusinessReportDto>.Succeeded(_report));
            AuthSecurityEventRecorder
                .Setup(r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    true,
                    null,
                    "book",
                    _ct,
                    200,
                    _report.Items.Count,
                    null,
                    null))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetBookOfBusinessReportRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_IsViewReport()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Same(_fixture._report, response.Result);
        }

        [Fact]
        public void Records_ReportExported_WithoutPhi()
        {
            _fixture.AuthSecurityEventRecorder.Verify(
                r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    true,
                    null,
                    "book",
                    _fixture._ct,
                    200,
                    _fixture._report.Items.Count,
                    null,
                    null),
                Times.Once);
        }
    }

    public abstract class Forbidden_Setup : ExportBookOfBusinessReportUseCaseTests, IAsyncLifetime
    {
        protected Forbidden_Setup(string role)
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, role);
            AuthSecurityEventRecorder
                .Setup(r => r.RecordAsync(
                    AuthSecurityEventTypes.ReportExported,
                    false,
                    "Export requires administrator role",
                    "book",
                    _ct,
                    403,
                    null,
                    null,
                    null))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetBookOfBusinessReportRequest>.From(_request, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class SuperAdminForbidden_Setup : Forbidden_Setup
    {
        public SuperAdminForbidden_Setup() : base(OrganizationRoles.SuperAdmin)
        {
        }
    }

    public sealed class SuperAdminForbidden : IClassFixture<SuperAdminForbidden_Setup>
    {
        private readonly SuperAdminForbidden_Setup _fixture;

        public SuperAdminForbidden(SuperAdminForbidden_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsForbidden()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        }

        [Fact]
        public void ViewUseCase_IsNotCalled()
        {
            _fixture.GetBookOfBusinessReportUseCase.Verify(
                u => u.Execute(It.IsAny<ProcessRequest<GetBookOfBusinessReportRequest>>(), It.IsAny<bool>()),
                Times.Never);
        }
    }

    public sealed class AgentForbidden_Setup : Forbidden_Setup
    {
        public AgentForbidden_Setup() : base(OrganizationRoles.Agent)
        {
        }
    }

    public sealed class AgentForbidden : IClassFixture<AgentForbidden_Setup>
    {
        private readonly AgentForbidden_Setup _fixture;

        public AgentForbidden(AgentForbidden_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsForbidden()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        }
    }

    public sealed class ReadOnlyForbidden_Setup : Forbidden_Setup
    {
        public ReadOnlyForbidden_Setup() : base(OrganizationRoles.ReadOnly)
        {
        }
    }

    public sealed class ReadOnlyForbidden : IClassFixture<ReadOnlyForbidden_Setup>
    {
        private readonly ReadOnlyForbidden_Setup _fixture;

        public ReadOnlyForbidden(ReadOnlyForbidden_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsForbidden()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Forbidden, response.Status);
        }
    }
}
