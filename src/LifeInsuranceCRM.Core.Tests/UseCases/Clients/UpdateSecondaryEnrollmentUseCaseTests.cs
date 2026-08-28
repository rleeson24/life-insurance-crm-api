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

public class UpdateSecondaryEnrollmentUseCaseTests : UseCaseTestBase<UpdateSecondaryEnrollmentUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly Guid _enrollmentId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly UpdateSecondaryEnrollmentModel _inputModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<ISecondaryEnrollmentRepository> SecondaryEnrollmentRepository => MockFor<ISecondaryEnrollmentRepository>();

    public UpdateSecondaryEnrollmentUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _enrollmentId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<UpdateSecondaryEnrollmentModel>() with
        {
            ClientId = _clientId,
            SecondaryEnrollmentId = _enrollmentId,
        };
    }

    protected override UpdateSecondaryEnrollmentUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            SecondaryEnrollmentRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers(),
            new SecondaryEnrollmentInputValidator());

    public sealed class Success_Setup : UpdateSecondaryEnrollmentUseCaseTests, IAsyncLifetime
    {
        private readonly SecondaryEnrollment _updatedEnrollment;

        public Success_Setup()
        {
            _updatedEnrollment = TestFixture.CreateSecondaryEnrollment(_enrollmentId, _clientId, _tenantId, _userId, _now);
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            SecondaryEnrollmentRepository
                .Setup(r => r.UpdateAsync(_inputModel, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_updatedEnrollment);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateSecondaryEnrollmentModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<SecondaryEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class InvalidEnrollmentId_Setup : UpdateSecondaryEnrollmentUseCaseTests, IAsyncLifetime
    {
        private readonly UpdateSecondaryEnrollmentModel _invalidModel;

        public InvalidEnrollmentId_Setup()
        {
            _invalidModel = _inputModel with { SecondaryEnrollmentId = Guid.Empty };
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateSecondaryEnrollmentModel>.From(_invalidModel, _ct));
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public sealed class InvalidEnrollmentId : IClassFixture<InvalidEnrollmentId_Setup>
    {
        private readonly InvalidEnrollmentId_Setup _fixture;

        public InvalidEnrollmentId(InvalidEnrollmentId_Setup fixture) => _fixture = fixture;

        [Fact]
        public void Status_IsInvalidRequest()
        {
            var response = (ProcessResponse<SecondaryEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        }

        [Fact]
        public void ErrorCode_IsSecondaryEnrollmentIdInvalid()
        {
            var response = (ProcessResponse<SecondaryEnrollmentDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.SecondaryEnrollmentIdInvalid, response.ErrorCode);
        }
    }
}
