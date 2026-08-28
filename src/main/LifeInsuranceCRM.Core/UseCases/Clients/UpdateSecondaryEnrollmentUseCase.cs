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

public interface IUpdateSecondaryEnrollmentUseCase
{
    Task<ProcessResponse<SecondaryEnrollmentDto>> Execute(ProcessRequest<UpdateSecondaryEnrollmentModel> request);
}

public sealed class UpdateSecondaryEnrollmentUseCase : IUpdateSecondaryEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly ISecondaryEnrollmentRepository _secondaryEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ISecondaryEnrollmentInputValidator _secondaryEnrollmentInputValidator;

    public UpdateSecondaryEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        ISecondaryEnrollmentRepository secondaryEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecondaryEnrollmentInputValidator secondaryEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _secondaryEnrollmentRepository = secondaryEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _secondaryEnrollmentInputValidator = secondaryEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<SecondaryEnrollmentDto>> Execute(ProcessRequest<UpdateSecondaryEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<SecondaryEnrollmentDto> failure))
        {
            return failure;
        }

        if (request.Payload.SecondaryEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<SecondaryEnrollmentDto>.InvalidRequestResponse(
                "Secondary enrollment id is required",
                ClientErrorCodes.SecondaryEnrollmentIdInvalid);
        }

        var inputValidation = _secondaryEnrollmentInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<SecondaryEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _secondaryEnrollmentRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (enrollment is null)
        {
            return ProcessResponse<SecondaryEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Secondary enrollment not found",
                ClientErrorCodes.SecondaryEnrollmentNotFound);
        }

        return ProcessResponse<SecondaryEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
