using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Services;
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
        new(ActorTracker.Object, ClientRepository.Object, new ClientUseCaseHelpers(), new SecurityAudit(NullAuthSecurityEventRecorder.Instance));

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

    [Fact]
    public async Task Execute_WhenClientsListed_RecordsPageCountsWithoutSearchText()
    {
        ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        var listed = new ListClientsResult
        {
            Items = [new ClientSummaryDto { ClientId = CreateGuid(), LastName = "Lee" }],
            TotalCount = 40,
            Page = 2,
            PageSize = 25,
        };
        var searchRequest = new ListClientsRequest { Search = "Pat Lee", Page = 2, PageSize = 25 };
        ClientRepository
            .Setup(r => r.ListAsync(searchRequest, _ct))
            .ReturnsAsync(listed);
        var recorder = new Mock<IAuthSecurityEventRecorder>();
        recorder
            .Setup(r => r.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var subject = new ListClientsUseCase(
            ActorTracker.Object,
            ClientRepository.Object,
            new ClientUseCaseHelpers(),
            new SecurityAudit(recorder.Object));

        var response = await subject.Execute(ProcessRequest<ListClientsRequest>.From(searchRequest, _ct));

        Assert.Equal(UseCaseStatus.Success, response.Status);
        recorder.Verify(
            r => r.RecordAsync(
                AuthSecurityEventTypes.ClientListed,
                true,
                null,
                "clients",
                _ct,
                200,
                1,
                null,
                "page=2;pageSize=25;total=40"),
            Times.Once);
    }
}
