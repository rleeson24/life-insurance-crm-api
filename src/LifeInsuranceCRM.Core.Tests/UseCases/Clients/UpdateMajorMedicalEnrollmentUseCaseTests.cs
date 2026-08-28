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

public class UpdateMajorMedicalEnrollmentUseCaseTests : UseCaseTestBase<UpdateMajorMedicalEnrollmentUseCase>
{
    private readonly Guid _tenantId;
    private readonly Guid _userId;
    private readonly Guid _clientId;
    private readonly Guid _enrollmentId;
    private readonly DateTimeOffset _now;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly UpdateMajorMedicalEnrollmentModel _inputModel;

    private Mock<IActorTracker> ActorTracker => MockFor<IActorTracker>();
    private Mock<INowProvider> NowProvider => MockFor<INowProvider>();
    private Mock<IMajorMedicalEnrollmentRepository> MajorMedicalEnrollmentRepository => MockFor<IMajorMedicalEnrollmentRepository>();

    public UpdateMajorMedicalEnrollmentUseCaseTests()
    {
        _tenantId = CreateGuid();
        _userId = CreateGuid();
        _clientId = CreateGuid();
        _enrollmentId = CreateGuid();
        _now = CreateTimestamp();
        _inputModel = Create<UpdateMajorMedicalEnrollmentModel>() with
        {
            ClientId = _clientId,
            MajorMedicalEnrollmentId = _enrollmentId,
        };
    }

    protected override UpdateMajorMedicalEnrollmentUseCase BuildSubject() =>
        new(
            ActorTracker.Object,
            NowProvider.Object,
            MajorMedicalEnrollmentRepository.Object,
            new ClientMapper(),
            new ClientUseCaseHelpers(),
            new MajorMedicalEnrollmentInputValidator());

    public sealed class Success_Setup : UpdateMajorMedicalEnrollmentUseCaseTests, IAsyncLifetime
    {
        private readonly MajorMedicalEnrollment _updatedEnrollment;

        public Success_Setup()
        {
            _updatedEnrollment = TestFixture.CreateMajorMedicalEnrollment(_enrollmentId, _clientId, _tenantId, _userId, _now);
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
            NowProvider.Setup(n => n.UtcNow).Returns(_now);
            MajorMedicalEnrollmentRepository
                .Setup(r => r.UpdateAsync(_inputModel, It.IsAny<AuditStamp>(), _ct))
                .ReturnsAsync(_updatedEnrollment);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateMajorMedicalEnrollmentModel>.From(_inputModel, _ct));
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
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.Success, response.Status);
        }
    }

    public sealed class InvalidEnrollmentId_Setup : UpdateMajorMedicalEnrollmentUseCaseTests, IAsyncLifetime
    {
        private readonly UpdateMajorMedicalEnrollmentModel _invalidModel;

        public InvalidEnrollmentId_Setup()
        {
            _invalidModel = _inputModel with { MajorMedicalEnrollmentId = Guid.Empty };
            ActorTracker.SetupAuthenticatedActor(_userId, _tenantId);
        }

        public async Task InitializeAsync()
        {
            await ExecuteOnceAsync(async subject =>
            {
                Result = await subject.Execute(ProcessRequest<UpdateMajorMedicalEnrollmentModel>.From(_invalidModel, _ct));
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
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        }

        [Fact]
        public void ErrorCode_IsMajorMedicalEnrollmentIdInvalid()
        {
            var response = (ProcessResponse<MajorMedicalEnrollmentDto>)_fixture.Result!;
            Assert.Equal(ClientErrorCodes.MajorMedicalEnrollmentIdInvalid, response.ErrorCode);
        }
    }
}
