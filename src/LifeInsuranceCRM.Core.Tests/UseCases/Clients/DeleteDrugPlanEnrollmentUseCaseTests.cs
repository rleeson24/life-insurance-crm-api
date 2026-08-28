using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class DeleteDrugPlanEnrollmentUseCaseTests : UseCaseTestBase<DeleteDrugPlanEnrollmentUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly Guid _enrollmentId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly DeleteDrugPlanEnrollmentRequest _request;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IDrugPlanEnrollmentRepository> DrugPlanEnrollmentRepository => MockFor<IDrugPlanEnrollmentRepository>();

    public DeleteDrugPlanEnrollmentUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _enrollmentId = CreateGuid();
        _now = CreateTimestamp();
        _request = new DeleteDrugPlanEnrollmentRequest
        {
            ClientId = _clientId,
            DrugPlanEnrollmentId = _enrollmentId,
        };
    }

    protected override DeleteDrugPlanEnrollmentUseCase BuildSubject() =>
        new(ActorTracker.Object, NowProvider.Object, DrugPlanEnrollmentRepository.Object, new ClientUseCaseHelpers());

    public sealed class Success_Setup : DeleteDrugPlanEnrollmentUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            DrugPlanEnrollmentRepository
                .Setup(r => r.SoftDeleteAsync(_clientId, _enrollmentId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(true);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<DeleteDrugPlanEnrollmentRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<bool>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class NotFound_Setup : DeleteDrugPlanEnrollmentUseCaseTests, IAsyncLifetime
    {
        public NotFound_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            DrugPlanEnrollmentRepository
                .Setup(r => r.SoftDeleteAsync(_clientId, _enrollmentId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(false);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<DeleteDrugPlanEnrollmentRequest>.From(_request, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class NotFound : IClassFixture<NotFound_Setup>
    {
        private readonly NotFound_Setup _fixture;

        public NotFound(NotFound_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsNotFound()
        {
            var response = (ProcessResponse<bool>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.NotFound, response.Status);
        }

        [Fact]
        public void ErrorCode_IsDrugPlanEnrollmentNotFound()
        {
            var response = (ProcessResponse<bool>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.DrugPlanEnrollmentNotFound, response.ErrorCode);
        }
    }
}
