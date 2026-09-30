using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Reports;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Reports;

public class GetRetentionReportUseCaseTests : UseCaseTestBase<GetRetentionReportUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetRetentionReportRequest _request = new();
    private readonly IReadOnlyList<RetentionReportRowDto> _rows;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IReportRepository> ReportRepository => MockFor<IReportRepository>();

    public GetRetentionReportUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _rows = [Create<RetentionReportRowDto>(), Create<RetentionReportRowDto>()];
    }

    protected override GetRetentionReportUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            ReportRepository.Object,
            new ReportUseCaseHelpers(new ClientUseCaseHelpers()),
            NullAuthSecurityEventRecorder.Instance);

    public sealed class Success_Setup : GetRetentionReportUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ReportRepository
                .Setup(r => r.ListRetentionAsync(_ct))
                .ReturnsAsync(_rows);
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
        public void Result_Rows_MatchRepository()
        {
            var response = (ProcessResponse<RetentionReportDto>)_fixture.Result!;
            Assert.Same(_fixture._rows, response.Result!.Rows);
        }
    }
}
