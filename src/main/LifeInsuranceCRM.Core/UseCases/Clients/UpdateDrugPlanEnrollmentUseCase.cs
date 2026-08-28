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

public interface IUpdateDrugPlanEnrollmentUseCase
{
    Task<ProcessResponse<DrugPlanEnrollmentDto>> Execute(ProcessRequest<UpdateDrugPlanEnrollmentModel> request);
}

public sealed class UpdateDrugPlanEnrollmentUseCase : IUpdateDrugPlanEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IDrugPlanEnrollmentRepository _drugPlanEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IDrugPlanEnrollmentInputValidator _drugPlanEnrollmentInputValidator;

    public UpdateDrugPlanEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IDrugPlanEnrollmentInputValidator drugPlanEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _drugPlanEnrollmentInputValidator = drugPlanEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<DrugPlanEnrollmentDto>> Execute(ProcessRequest<UpdateDrugPlanEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<DrugPlanEnrollmentDto> failure))
        {
            return failure;
        }

        if (request.Payload.DrugPlanEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<DrugPlanEnrollmentDto>.InvalidRequestResponse(
                "Drug plan enrollment id is required",
                ClientErrorCodes.DrugPlanEnrollmentIdInvalid);
        }

        var inputValidation = _drugPlanEnrollmentInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<DrugPlanEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _drugPlanEnrollmentRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (enrollment is null)
        {
            return ProcessResponse<DrugPlanEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Drug plan enrollment not found",
                ClientErrorCodes.DrugPlanEnrollmentNotFound);
        }

        return ProcessResponse<DrugPlanEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
