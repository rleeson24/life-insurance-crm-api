using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.PlanNames;

public interface ICreatePlanNameUseCase
{
    Task<ProcessResponse<PlanNameDto>> Execute(ProcessRequest<CreatePlanNameModel> request);
}

public sealed class CreatePlanNameUseCase : ICreatePlanNameUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IPlanNameRepository _planNameRepository;
    private readonly IPlanNameMapper _planNameMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IPlanNameInputValidator _planNameInputValidator;

    public CreatePlanNameUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IPlanNameRepository planNameRepository,
        IPlanNameMapper planNameMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IPlanNameInputValidator planNameInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _planNameRepository = planNameRepository;
        _planNameMapper = planNameMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _planNameInputValidator = planNameInputValidator;
    }

    public async Task<ProcessResponse<PlanNameDto>> Execute(ProcessRequest<CreatePlanNameModel> request)
    {
        var validation = PlanNameUseCaseHelpers.ValidateActor(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<PlanNameDto> failure))
        {
            return failure;
        }

        var inputValidation = _planNameInputValidator.ValidateCreate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<PlanNameDto> inputFailure))
        {
            return inputFailure;
        }

        var name = request.Payload.Name!.Trim();
        if (await _planNameRepository.ExistsByNameAsync(
                request.Payload.Kind,
                request.Payload.PlanYear,
                name,
                excludePlanNameId: null,
                request.CancellationToken))
        {
            return ProcessResponse<PlanNameDto>.WithStatus(
                UseCaseStatus.Conflict,
                "That plan name already exists for this year",
                PlanNameErrorCodes.NameAlreadyExists);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var created = await _planNameRepository.InsertAsync(
            request.Payload.Kind,
            _actorTracker.TenantId!.Value,
            request.Payload.PlanYear,
            name,
            audit,
            request.CancellationToken);

        return ProcessResponse<PlanNameDto>.Succeeded(_planNameMapper.ToDto(created));
    }
}
