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

public interface ICreateDrugPlanEnrollmentUseCase
{
    Task<ProcessResponse<DrugPlanEnrollmentDto>> Execute(ProcessRequest<CreateDrugPlanEnrollmentModel> request);
}

public sealed class CreateDrugPlanEnrollmentUseCase : ICreateDrugPlanEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IDrugPlanEnrollmentRepository _drugPlanEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IDrugPlanEnrollmentInputValidator _drugPlanEnrollmentInputValidator;

    public CreateDrugPlanEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IDrugPlanEnrollmentInputValidator drugPlanEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _drugPlanEnrollmentInputValidator = drugPlanEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<DrugPlanEnrollmentDto>> Execute(ProcessRequest<CreateDrugPlanEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<DrugPlanEnrollmentDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<DrugPlanEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var inputValidation = _drugPlanEnrollmentInputValidator.ValidateCreate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<DrugPlanEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _drugPlanEnrollmentRepository.InsertAsync(
            request.Payload,
            _actorTracker.TenantId!.Value,
            audit,
            request.CancellationToken);

        return ProcessResponse<DrugPlanEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
