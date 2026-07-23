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

public class CreateSupplementalEnrollmentUseCaseTests : UseCaseTestBase<CreateSupplementalEnrollmentUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly CreateSupplementalEnrollmentModel _inputModel;
    private readonly SupplementalEnrollment _createdEnrollment;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();
    private Mock<ISupplementalEnrollmentRepository> SupplementalEnrollmentRepository => MockFor<ISupplementalEnrollmentRepository>();

    public CreateSupplementalEnrollmentUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<CreateSupplementalEnrollmentModel>() with { ClientId = _clientId };
        var enrollmentId = CreateGuid();
        _createdEnrollment = TestFixture.CreateSupplementalEnrollment(enrollmentId, _clientId, _tenantId, _userId, _now);
    }

    protected override CreateSupplementalEnrollmentUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            ClientRepository.Object,
            SupplementalEnrollmentRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers(),
            new SupplementalEnrollmentInputValidator());

    public sealed class Success_Setup : CreateSupplementalEnrollmentUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.GetByIdAsync(_clientId, _ct))
                .ReturnsAsync(TestFixture.CreateClient(_clientId, _tenantId, _userId, _now));
            SupplementalEnrollmentRepository
                .Setup(r => r.InsertAsync(_inputModel, _tenantId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_createdEnrollment);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<CreateSupplementalEnrollmentModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_EnrollmentId_MatchesRepository()
        {
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(_fixture._createdEnrollment.SupplementalEnrollmentId, response.Result!.SupplementalEnrollmentId);
        }
    }

    public sealed class ClientNotFound_Setup : CreateSupplementalEnrollmentUseCaseTests, IAsyncLifetime
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
                Result = await subject.Execute(ProcessRequest<CreateSupplementalEnrollmentModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.NotFound, response.Status);
        }

        [Fact]
        public void ErrorCode_IsClientNotFound()
        {
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ClientNotFound, response.ErrorCode);
        }
    }
}
