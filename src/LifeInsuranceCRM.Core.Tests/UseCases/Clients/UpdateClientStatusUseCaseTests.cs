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
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class UpdateClientStatusUseCaseTests : UseCaseTestBase<UpdateClientStatusUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly UpdateClientStatusModel _inputModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();

    public UpdateClientStatusUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<UpdateClientStatusModel>() with { ClientId = _clientId };
    }

    protected override UpdateClientStatusUseCase BuildSubject() =>
        new(ActorTracker.Object, NowProvider.Object, ClientRepository.Object, new ClientMapper(), new ClientUseCaseHelpers());

    public sealed class Success_Setup : UpdateClientStatusUseCaseTests, IAsyncLifetime
    {
        private readonly Client _updatedClient;

        public Success_Setup()
        {
            _updatedClient = TestFixture.CreateClient(_clientId, _tenantId, _userId, _now);
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.UpdateStatusAsync(_inputModel, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_updatedClient);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateClientStatusModel>.From(_inputModel, _ct));
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
    }

    public sealed class NotFound_Setup : UpdateClientStatusUseCaseTests, IAsyncLifetime
    {
        public NotFound_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.UpdateStatusAsync(_inputModel, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync((Client?)null);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateClientStatusModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<ClientDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.NotFound, response.Status);
        }

        [Fact]
        public void ErrorCode_IsClientNotFound()
        {
            var response = (ProcessResponse<ClientDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ClientNotFound, response.ErrorCode);
        }
    }
}
