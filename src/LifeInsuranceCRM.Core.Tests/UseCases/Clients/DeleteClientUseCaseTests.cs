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

public class DeleteClientUseCaseTests : UseCaseTestBase<DeleteClientUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly DeleteClientRequest _request;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IClientRepository> ClientRepository => MockFor<IClientRepository>();

    public DeleteClientUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _now = CreateTimestamp();
        _request = new DeleteClientRequest { ClientId = _clientId };
    }

    protected override DeleteClientUseCase BuildSubject() =>
        new(ActorTracker.Object, NowProvider.Object, ClientRepository.Object, new ClientUseCaseHelpers());

    public sealed class Success_Setup : DeleteClientUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.SoftDeleteAsync(_clientId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(true);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<DeleteClientRequest>.From(_request, _ct));
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

        [Fact]
        public void Result_IsTrue()
        {
            var response = (ProcessResponse<bool>)_fixture.Result!;
            Assert.True(response.Result);
        }
    }

    public sealed class NotFound_Setup : DeleteClientUseCaseTests, IAsyncLifetime
    {
        public NotFound_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            ClientRepository
                .Setup(r => r.SoftDeleteAsync(_clientId, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(false);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<DeleteClientRequest>.From(_request, _ct));
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
        public void ErrorCode_IsClientNotFound()
        {
            var response = (ProcessResponse<bool>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.ClientNotFound, response.ErrorCode);
        }
    }
}
