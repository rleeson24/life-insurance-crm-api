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

public class UpdateClientInteractionUseCaseTests : UseCaseTestBase<UpdateClientInteractionUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly Guid _interactionId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly UpdateClientInteractionModel _inputModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IClientInteractionRepository> ClientInteractionRepository => MockFor<IClientInteractionRepository>();

    public UpdateClientInteractionUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _interactionId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<UpdateClientInteractionModel>() with
        {
            ClientId = _clientId,
            ClientInteractionId = _interactionId,
        };
    }

    protected override UpdateClientInteractionUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            ClientInteractionRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers());

    public sealed class Success_Setup : UpdateClientInteractionUseCaseTests, IAsyncLifetime
    {
        private readonly ClientInteraction _updatedInteraction;

        public Success_Setup()
        {
            _updatedInteraction = TestFixture.CreateClientInteraction(_interactionId, _clientId, _tenantId, _userId, _now);
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientInteractionRepository
                .Setup(r => r.UpdateAsync(_inputModel, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_updatedInteraction);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateClientInteractionModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<ClientInteractionDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class InvalidInteractionId_Setup : UpdateClientInteractionUseCaseTests, IAsyncLifetime
    {
        private readonly UpdateClientInteractionModel _invalidModel;

        public InvalidInteractionId_Setup()
        {
            _invalidModel = _inputModel with { ClientInteractionId = Guid.Empty };
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateClientInteractionModel>.From(_invalidModel, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class InvalidInteractionId : IClassFixture<InvalidInteractionId_Setup>
    {
        private readonly InvalidInteractionId_Setup _fixture;

        public InvalidInteractionId(InvalidInteractionId_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsInvalidRequest()
        {
            var response = (ProcessResponse<ClientInteractionDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        }

        [Fact]
        public void ErrorCode_IsInteractionIdInvalid()
        {
            var response = (ProcessResponse<ClientInteractionDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.InteractionIdInvalid, response.ErrorCode);
        }
    }
}
