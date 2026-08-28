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

public interface IUpdateMajorMedicalEnrollmentUseCase
{
    Task<ProcessResponse<MajorMedicalEnrollmentDto>> Execute(ProcessRequest<UpdateMajorMedicalEnrollmentModel> request);
}

public sealed class UpdateMajorMedicalEnrollmentUseCase : IUpdateMajorMedicalEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IMajorMedicalEnrollmentRepository _majorMedicalEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IMajorMedicalEnrollmentInputValidator _majorMedicalEnrollmentInputValidator;

    public UpdateMajorMedicalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IMajorMedicalEnrollmentRepository majorMedicalEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IMajorMedicalEnrollmentInputValidator majorMedicalEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _majorMedicalEnrollmentRepository = majorMedicalEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _majorMedicalEnrollmentInputValidator = majorMedicalEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<MajorMedicalEnrollmentDto>> Execute(ProcessRequest<UpdateMajorMedicalEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<MajorMedicalEnrollmentDto> failure))
        {
            return failure;
        }

        if (request.Payload.MajorMedicalEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<MajorMedicalEnrollmentDto>.InvalidRequestResponse(
                "Major Medical enrollment id is required",
                ClientErrorCodes.MajorMedicalEnrollmentIdInvalid);
        }

        var inputValidation = _majorMedicalEnrollmentInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<MajorMedicalEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _majorMedicalEnrollmentRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (enrollment is null)
        {
            return ProcessResponse<MajorMedicalEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Major Medical enrollment not found",
                ClientErrorCodes.MajorMedicalEnrollmentNotFound);
        }

        return ProcessResponse<MajorMedicalEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
