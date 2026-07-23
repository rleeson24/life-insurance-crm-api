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

public interface IUpdateSupplementalEnrollmentUseCase
{
    Task<ProcessResponse<SupplementalEnrollmentDto>> Execute(ProcessRequest<UpdateSupplementalEnrollmentModel> request);
}

public sealed class UpdateSupplementalEnrollmentUseCase : IUpdateSupplementalEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly ISupplementalEnrollmentRepository _supplementalEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ISupplementalEnrollmentInputValidator _supplementalEnrollmentInputValidator;

    public UpdateSupplementalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        ISupplementalEnrollmentRepository supplementalEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISupplementalEnrollmentInputValidator supplementalEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _supplementalEnrollmentRepository = supplementalEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _supplementalEnrollmentInputValidator = supplementalEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<SupplementalEnrollmentDto>> Execute(ProcessRequest<UpdateSupplementalEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<SupplementalEnrollmentDto> failure))
        {
            return failure;
        }

        if (request.Payload.SupplementalEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<SupplementalEnrollmentDto>.InvalidRequestResponse(
                "Supplemental enrollment id is required",
                ClientErrorCodes.SupplementalEnrollmentIdInvalid);
        }

        var inputValidation = _supplementalEnrollmentInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<SupplementalEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _supplementalEnrollmentRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (enrollment is null)
        {
            return ProcessResponse<SupplementalEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Supplemental enrollment not found",
                ClientErrorCodes.SupplementalEnrollmentNotFound);
        }

        return ProcessResponse<SupplementalEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
