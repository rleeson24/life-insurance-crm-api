using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class ListClientsUseCaseTests : UseCaseTestBase<ListClientsUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly ListClientsRequest _request;
    private readonly ListClientsResult _listResult;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();

    public ListClientsUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _request = new ListClientsRequest { Page = 1, PageSize = 25 };
        _listResult = Create<ListClientsResult>();
    }

    protected override ListClientsUseCase BuildSubject() =>
        new(ActorTracker.Object, ClientRepository.Object, new ClientUseCaseHelpers());

    public sealed class Success_Setup : ListClientsUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ClientRepository
                .Setup(r => r.ListAsync(_request, _ct))
                .ReturnsAsync(_listResult);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<ListClientsRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<ListClientsResult>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_MatchesRepository()
        {
            var response = (ProcessResponse<ListClientsResult>)_fixture.Result!;
            Assert.Same(_fixture._listResult, response.Result);
        }
    }

    public sealed class Unauthorized_Setup : ListClientsUseCaseTests, IAsyncLifetime
    {
        public Unauthorized_Setup()
        {
            ActorTracker.SetupUnauthenticatedActor();
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<ListClientsRequest>.From(_request, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class Unauthorized : IClassFixture<Unauthorized_Setup>
    {
        private readonly Unauthorized_Setup _fixture;

        public Unauthorized(Unauthorized_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsUnauthorized()
        {
            var response = (ProcessResponse<ListClientsResult>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Unauthorized, response.Status);
        }

        [Fact]
        public void ErrorCode_IsActorNotAuthenticated()
        {
            var response = (ProcessResponse<ListClientsResult>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ActorNotAuthenticated, response.ErrorCode);
        }
    }
}
