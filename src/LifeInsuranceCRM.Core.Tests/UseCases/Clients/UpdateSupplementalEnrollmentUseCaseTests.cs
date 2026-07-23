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

public class UpdateSupplementalEnrollmentUseCaseTests : UseCaseTestBase<UpdateSupplementalEnrollmentUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly Guid _enrollmentId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly UpdateSupplementalEnrollmentModel _inputModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<ISupplementalEnrollmentRepository> SupplementalEnrollmentRepository => MockFor<ISupplementalEnrollmentRepository>();

    public UpdateSupplementalEnrollmentUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _enrollmentId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<UpdateSupplementalEnrollmentModel>() with
        {
            ClientId = _clientId,
            SupplementalEnrollmentId = _enrollmentId,
        };
    }

    protected override UpdateSupplementalEnrollmentUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            SupplementalEnrollmentRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers(),
            new SupplementalEnrollmentInputValidator());

    public sealed class Success_Setup : UpdateSupplementalEnrollmentUseCaseTests, IAsyncLifetime
    {
        private readonly SupplementalEnrollment _updatedEnrollment;

        public Success_Setup()
        {
            _updatedEnrollment = TestFixture.CreateSupplementalEnrollment(_enrollmentId, _clientId, _tenantId, _userId, _now);
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            SupplementalEnrollmentRepository
                .Setup(r => r.UpdateAsync(_inputModel, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_updatedEnrollment);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateSupplementalEnrollmentModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class InvalidEnrollmentId_Setup : UpdateSupplementalEnrollmentUseCaseTests, IAsyncLifetime
    {
        private readonly UpdateSupplementalEnrollmentModel _invalidModel;

        public InvalidEnrollmentId_Setup()
        {
            _invalidModel = _inputModel with { SupplementalEnrollmentId = Guid.Empty };
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateSupplementalEnrollmentModel>.From(_invalidModel, _ct));
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
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        }

        [Fact]
        public void ErrorCode_IsSupplementalEnrollmentIdInvalid()
        {
            var response = (ProcessResponse<SupplementalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.SupplementalEnrollmentIdInvalid, response.ErrorCode);
        }
    }
}
