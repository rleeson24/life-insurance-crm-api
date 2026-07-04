using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Tests.Utilities;
using LifeInsuranceCRM.Utilities;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.UseCases.Clients;

public class ListFollowUpInteractionsUseCaseTests : UseCaseTestBase<ListFollowUpInteractionsUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly ListFollowUpInteractionsRequest _request;
    private readonly IReadOnlyList<FollowUpInteractionDto> _followUps;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<IClientInteractionRepository> ClientInteractionRepository => MockFor<IClientInteractionRepository>();

    public ListFollowUpInteractionsUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _request = new ListFollowUpInteractionsRequest();
        _followUps =
        [
            Create<FollowUpInteractionDto>(),
            Create<FollowUpInteractionDto>(),
        ];
    }

    protected override ListFollowUpInteractionsUseCase BuildSubject() =>
        new(ActorTracker.Object, ClientInteractionRepository.Object, new ClientUseCaseHelpers());

    public sealed class Success_Setup : ListFollowUpInteractionsUseCaseTests, IAsyncLifetime
    {
        public Success_Setup()
        {
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            ClientInteractionRepository
                .Setup(r => r.ListFollowUpsAsync(_ct))
                .ReturnsAsync(_followUps);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<ListFollowUpInteractionsRequest>.From(_request, _ct));
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
            var response = (ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }

        [Fact]
        public void Result_Count_MatchesRepository()
        {
            var response = (ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>)_fixture.Result!;
            Assert.Equal(2, response.Result!.Count);
        }
    }
}
