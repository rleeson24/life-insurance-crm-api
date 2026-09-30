using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
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

public class GetBookOfBusinessReportUseCaseTests : UseCaseTestBase<GetBookOfBusinessReportUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetBookOfBusinessReportRequest _request = new();
    private readonly BookOfBusinessReportDto _report;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IReportRepository> ReportRepository => MockFor<IReportRepository>();

    public GetBookOfBusinessReportUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _report = Create<BookOfBusinessReportDto>();
    }

    protected override GetBookOfBusinessReportUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            ReportRepository.Object,
            new ReportUseCaseHelpers(new ClientUseCaseHelpers()),
            new SecurityAudit(NullAuthSecurityEventRecorder.Instance));

    public abstract class ViewerSuccess_Setup : GetBookOfBusinessReportUseCaseTests, IAsyncLifetime
    {
        protected ViewerSuccess_Setup(string role)
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId, role);
            ReportRepository
                .Setup(r => r.GetBookOfBusinessAsync(_ct))
                .ReturnsAsync(_report);
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

    public sealed class AdminSuccess_Setup : ViewerSuccess_Setup
    {
        public AdminSuccess_Setup() : base(OrganizationRoles.Admin)
        {
        }
    }

    public sealed class AdminSuccess : IClassFixture<AdminSuccess_Setup>
    {
        private readonly AdminSuccess_Setup _fixture;

        public AdminSuccess(AdminSuccess_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsSuccess()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_IsRepositoryReport()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Same(_fixture._report, response.Result);
        }
    }

    public sealed class SuperAdminSuccess_Setup : ViewerSuccess_Setup
    {
        public SuperAdminSuccess_Setup() : base(OrganizationRoles.SuperAdmin)
        {
        }
    }

    public sealed class SuperAdminSuccess : IClassFixture<SuperAdminSuccess_Setup>
    {
        private readonly SuperAdminSuccess_Setup _fixture;

        public SuperAdminSuccess(SuperAdminSuccess_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsSuccess()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class AgentSuccess_Setup : ViewerSuccess_Setup
    {
        public AgentSuccess_Setup() : base(OrganizationRoles.Agent)
        {
        }
    }

    public sealed class AgentSuccess : IClassFixture<AgentSuccess_Setup>
    {
        private readonly AgentSuccess_Setup _fixture;

        public AgentSuccess(AgentSuccess_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsSuccess()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class ReadOnlySuccess_Setup : ViewerSuccess_Setup
    {
        public ReadOnlySuccess_Setup() : base(OrganizationRoles.ReadOnly)
        {
        }
    }

    public sealed class ReadOnlySuccess : IClassFixture<ReadOnlySuccess_Setup>
    {
        private readonly ReadOnlySuccess_Setup _fixture;

        public ReadOnlySuccess(ReadOnlySuccess_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsSuccess()
        {
            var response = (ProcessResponse<BookOfBusinessReportDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }
}
