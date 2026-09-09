using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Reports;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Reports;

public class GetProductionReportUseCaseTests : UseCaseTestBase<GetProductionReportUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetProductionReportRequest _request;
    private readonly IReadOnlyList<ProductionReportRowDto> _medicare;
    private readonly IReadOnlyList<ProductionReportRowDto> _drug;
    private readonly IReadOnlyList<ProductionReportRowDto> _secondary;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IReportRepository> ReportRepository => MockFor<IReportRepository>();

    public GetProductionReportUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _request = new GetProductionReportRequest { PlanYear = 2026 };
        _medicare = [Create<ProductionReportRowDto>()];
        _drug = [Create<ProductionReportRowDto>()];
        _secondary = [Create<ProductionReportRowDto>()];
    }

    protected override GetProductionReportUseCase BuildSubject() =>
        new(ActorTracker.Object, ReportRepository.Object, new ReportUseCaseHelpers(new ClientUseCaseHelpers()));

    public sealed class Success_Setup : GetProductionReportUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ReportRepository
                .Setup(r => r.ListMedicareProductionAsync(_request.PlanYear, _ct))
                .ReturnsAsync(_medicare);
            ReportRepository
                .Setup(r => r.ListDrugProductionAsync(_request.PlanYear, _ct))
                .ReturnsAsync(_drug);
            ReportRepository
                .Setup(r => r.ListSecondaryProductionAsync(_request.PlanYear, _ct))
                .ReturnsAsync(_secondary);
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
        public void Result_PlanYear_MatchesRequest()
        {
            var response = (ProcessResponse<ProductionReportDto>)_fixture.Result!;
            Assert.Equal(_fixture._request.PlanYear, response.Result!.PlanYear);
        }

        [Fact]
        public void Result_Medicare_IsRepositoryList()
        {
            var response = (ProcessResponse<ProductionReportDto>)_fixture.Result!;
            Assert.Same(_fixture._medicare, response.Result!.Medicare);
        }
    }

    public sealed class InvalidPlanYear_Setup : GetProductionReportUseCaseTests, IAsyncLifetime
    {
        public InvalidPlanYear_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        }

        public async Task InitializeAsync()
        {
            var request = new GetProductionReportRequest { PlanYear = 1800 };
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetProductionReportRequest>.From(request, _ct));
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
        public void ErrorCode_IsPlanYearInvalid()
        {
            var response = (ProcessResponse<ProductionReportDto>)_fixture.Result!;
            Assert.Equal(ReportErrorCodes.PlanYearInvalid, response.ErrorCode);
        }

        [Fact]
        public void Repository_IsNotCalled()
        {
            _fixture.ReportRepository.Verify(
                r => r.ListMedicareProductionAsync(It.IsAny<short>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
