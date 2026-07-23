using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class CreateClientUseCaseTests : UseCaseTestBase<CreateClientUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly CreateClientModel _inputModel;
    private readonly Client _createdClient;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();

    public CreateClientUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = TestFixture.CreateValidCreateClientModel();
        _createdClient = TestFixture.CreateClient(
            CreateGuid(),
            _tenantId,
            _userId,
            _now,
            _inputModel.FirstName,
            _inputModel.LastName);
    }

    protected override CreateClientUseCase BuildSubject() =>
        new(ActorTracker.Object, NowProvider.Object, ClientRepository.Object, new ClientMapper(), new ClientUseCaseHelpers(), new ClientInputValidator());

    public sealed class Success_Setup : CreateClientUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.InsertAsync(_inputModel, _tenantId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_createdClient);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<CreateClientModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<ClientDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_ClientId_MatchesRepository()
        {
            var response = (ProcessResponse<ClientDto>)_fixture.Result!;
            Assert.Equal(_fixture._createdClient.ClientId, response.Result!.ClientId);
        }
    }

    public sealed class MissingFirstName_Setup : CreateClientUseCaseTests, IAsyncLifetime
    {
        private readonly CreateClientModel _invalidModel;

        public MissingFirstName_Setup()
        {
            _invalidModel = _inputModel with { FirstName = "  " };
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<CreateClientModel>.From(_invalidModel, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class MissingFirstName : IClassFixture<MissingFirstName_Setup>
    {
        private readonly MissingFirstName_Setup _fixture;

        public MissingFirstName(MissingFirstName_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsInvalidRequest()
        {
            var response = (ProcessResponse<ClientDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        }

        [Fact]
        public void ErrorCode_IsFirstNameRequired()
        {
            var response = (ProcessResponse<ClientDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.FirstNameRequired, response.ErrorCode);
        }
    }
}
