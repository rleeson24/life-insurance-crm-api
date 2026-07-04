using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class ListClientInteractionsUseCaseTests : UseCaseTestBase<ListClientInteractionsUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly ListClientInteractionsRequest _request;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();
    private Mock<IClientInteractionRepository> ClientInteractionRepository => MockFor<IClientInteractionRepository>();

    public ListClientInteractionsUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _request = new ListClientInteractionsRequest { ClientId = _clientId };
    }

    protected override ListClientInteractionsUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            ClientRepository.Object,
            ClientInteractionRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers());

    public sealed class Success_Setup : ListClientInteractionsUseCaseTests, IAsyncLifetime
    {
        private readonly IReadOnlyList<ClientInteraction> _interactions;

        public Success_Setup()
        {
            var now = CreateTimestamp();
            var interactionId = CreateGuid();
            _interactions = [TestFixture.CreateClientInteraction(interactionId, _clientId, _tenantId, _userId, now)];
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ClientRepository
                .Setup(r => r.GetByIdAsync(_clientId, _ct))
                .ReturnsAsync(TestFixture.CreateClient(_clientId, _tenantId, _userId, now));
            ClientInteractionRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(_interactions);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<ListClientInteractionsRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<IReadOnlyList<ClientInteractionDto>>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_Count_MatchesRepository()
        {
            var response = (ProcessResponse<IReadOnlyList<ClientInteractionDto>>)_fixture.Result!;
            Assert.Single(response.Result!);
        }
    }

    public sealed class ClientNotFound_Setup : ListClientInteractionsUseCaseTests, IAsyncLifetime
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
                Result = await subject.Execute(ProcessRequest<ListClientInteractionsRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<IReadOnlyList<ClientInteractionDto>>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.NotFound, response.Status);
        }

        [Fact]
        public void ErrorCode_IsClientNotFound()
        {
            var response = (ProcessResponse<IReadOnlyList<ClientInteractionDto>>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ClientNotFound, response.ErrorCode);
        }
    }
}
