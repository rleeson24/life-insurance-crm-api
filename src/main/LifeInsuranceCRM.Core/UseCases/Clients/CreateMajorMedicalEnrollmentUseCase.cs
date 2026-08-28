using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface ICreateMajorMedicalEnrollmentUseCase
{
    Task<ProcessResponse<MajorMedicalEnrollmentDto>> Execute(ProcessRequest<CreateMajorMedicalEnrollmentModel> request);
}

public sealed class CreateMajorMedicalEnrollmentUseCase : ICreateMajorMedicalEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IMajorMedicalEnrollmentRepository _majorMedicalEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IMajorMedicalEnrollmentInputValidator _majorMedicalEnrollmentInputValidator;

    public CreateMajorMedicalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IMajorMedicalEnrollmentRepository majorMedicalEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IMajorMedicalEnrollmentInputValidator majorMedicalEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _majorMedicalEnrollmentRepository = majorMedicalEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _majorMedicalEnrollmentInputValidator = majorMedicalEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<MajorMedicalEnrollmentDto>> Execute(ProcessRequest<CreateMajorMedicalEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<MajorMedicalEnrollmentDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<MajorMedicalEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var inputValidation = _majorMedicalEnrollmentInputValidator.ValidateCreate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<MajorMedicalEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _majorMedicalEnrollmentRepository.InsertAsync(
            request.Payload,
            _actorTracker.TenantId!.Value,
            audit,
            request.CancellationToken);

        return ProcessResponse<MajorMedicalEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
