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

public class GetClientDetailUseCaseTests : UseCaseTestBase<GetClientDetailUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly GetClientDetailRequest _request;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();
    private Mock<IClientInteractionRepository> ClientInteractionRepository => MockFor<IClientInteractionRepository>();
    private Mock<IMedicareEnrollmentRepository> MedicareEnrollmentRepository => MockFor<IMedicareEnrollmentRepository>();
    private Mock<ISupplementalEnrollmentRepository> SupplementalEnrollmentRepository => MockFor<ISupplementalEnrollmentRepository>();

    public GetClientDetailUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _request = new GetClientDetailRequest { ClientId = _clientId };
    }

    protected override GetClientDetailUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            ClientRepository.Object,
            ClientInteractionRepository.Object,
            MedicareEnrollmentRepository.Object,
            SupplementalEnrollmentRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers());

    public sealed class Success_Setup : GetClientDetailUseCaseTests, IAsyncLifetime
    {
        private readonly Client _client;

        public Success_Setup()
        {
            var now = CreateTimestamp();
            _client = TestFixture.CreateClient(_clientId, _tenantId, _userId, now);
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ClientRepository
                .Setup(r => r.GetByIdAsync(_clientId, _ct))
                .ReturnsAsync(_client);
            ClientInteractionRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(Array.Empty<ClientInteraction>());
            MedicareEnrollmentRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(Array.Empty<MedicareEnrollment>());
            SupplementalEnrollmentRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(Array.Empty<SupplementalEnrollment>());
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetClientDetailRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<ClientDetailDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_ClientId_MatchesRequest()
        {
            var response = (ProcessResponse<ClientDetailDto>)_fixture.Result!;
            Assert.Equal(_fixture._clientId, response.Result!.Client.ClientId);
        }
    }

    public sealed class NotFound_Setup : GetClientDetailUseCaseTests, IAsyncLifetime
    {
        public NotFound_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ClientRepository
                .Setup(r => r.GetByIdAsync(_clientId, _ct))
                .ReturnsAsync((Client?)null);
            ClientInteractionRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(Array.Empty<ClientInteraction>());
            MedicareEnrollmentRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(Array.Empty<MedicareEnrollment>());
            SupplementalEnrollmentRepository
                .Setup(r => r.ListByClientIdAsync(_clientId, _ct))
                .ReturnsAsync(Array.Empty<SupplementalEnrollment>());
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<GetClientDetailRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<ClientDetailDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.NotFound, response.Status);
        }

        [Fact]
        public void ErrorCode_IsClientNotFound()
        {
            var response = (ProcessResponse<ClientDetailDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ClientNotFound, response.ErrorCode);
        }
    }
}
