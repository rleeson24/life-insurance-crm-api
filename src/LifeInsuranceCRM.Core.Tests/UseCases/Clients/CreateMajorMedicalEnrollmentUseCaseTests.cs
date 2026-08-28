using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class CreateMajorMedicalEnrollmentUseCaseTests : UseCaseTestBase<CreateMajorMedicalEnrollmentUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly CreateMajorMedicalEnrollmentModel _inputModel;
    private readonly MajorMedicalEnrollment _createdEnrollment;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();
    private Mock<IMajorMedicalEnrollmentRepository> MajorMedicalEnrollmentRepository => MockFor<IMajorMedicalEnrollmentRepository>();

    public CreateMajorMedicalEnrollmentUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<CreateMajorMedicalEnrollmentModel>() with { ClientId = _clientId };
        var enrollmentId = CreateGuid();
        _createdEnrollment = TestFixture.CreateMajorMedicalEnrollment(enrollmentId, _clientId, _tenantId, _userId, _now);
    }

    protected override CreateMajorMedicalEnrollmentUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            ClientRepository.Object,
            MajorMedicalEnrollmentRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers(),
            new MajorMedicalEnrollmentInputValidator());

    public sealed class Success_Setup : CreateMajorMedicalEnrollmentUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.GetByIdAsync(_clientId, _ct))
                .ReturnsAsync(TestFixture.CreateClient(_clientId, _tenantId, _userId, _now));
            MajorMedicalEnrollmentRepository
                .Setup(r => r.InsertAsync(_inputModel, _tenantId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_createdEnrollment);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<CreateMajorMedicalEnrollmentModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_EnrollmentId_MatchesRepository()
        {
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(_fixture._createdEnrollment.MajorMedicalEnrollmentId, response.Result!.MajorMedicalEnrollmentId);
        }
    }

    public sealed class ClientNotFound_Setup : CreateMajorMedicalEnrollmentUseCaseTests, IAsyncLifetime
    {
        public ClientNotFound_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ClientRepository
                .Setup(r => r.GetByIdAsync(_clientId, _ct))
                .ReturnsAsync((Client?)null);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<CreateMajorMedicalEnrollmentModel>.From(_inputModel, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class ClientNotFound : IClassFixture<ClientNotFound_Setup>
    {
        private readonly ClientNotFound_Setup _fixture;

        public ClientNotFound(ClientNotFound_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsNotFound()
        {
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.NotFound, response.Status);
        }

        [Fact]
        public void ErrorCode_IsClientNotFound()
        {
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ClientNotFound, response.ErrorCode);
        }
    }
}
