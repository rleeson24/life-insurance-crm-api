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

public interface IUpdatePlanNameUseCase
{
    Task<ProcessResponse<PlanNameDto>> Execute(ProcessRequest<UpdatePlanNameModel> request);
}

public sealed class UpdatePlanNameUseCase : IUpdatePlanNameUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IPlanNameRepository _planNameRepository;
    private readonly IPlanNameMapper _planNameMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IPlanNameInputValidator _planNameInputValidator;

    public UpdatePlanNameUseCase(
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

    public async Task<ProcessResponse<PlanNameDto>> Execute(ProcessRequest<UpdatePlanNameModel> request)
    {
        var validation = PlanNameUseCaseHelpers.ValidateAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<PlanNameDto> failure))
        {
            return failure;
        }

        var inputValidation = _planNameInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<PlanNameDto> inputFailure))
        {
            return inputFailure;
        }

        var existing = await _planNameRepository.GetByIdAsync(
            request.Payload.Kind,
            request.Payload.PlanNameId,
            request.CancellationToken);
        if (existing is null)
        {
            return ProcessResponse<PlanNameDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Plan name not found",
                PlanNameErrorCodes.NotFound);
        }

        var name = request.Payload.Name!.Trim();
        if (await _planNameRepository.ExistsByNameAsync(
                request.Payload.Kind,
                existing.PlanYear,
                name,
                request.Payload.PlanNameId,
                request.CancellationToken))
        {
            return ProcessResponse<PlanNameDto>.WithStatus(
                UseCaseStatus.Conflict,
                "That plan name already exists for this year",
                PlanNameErrorCodes.NameAlreadyExists);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var updated = await _planNameRepository.UpdateNameAsync(
            request.Payload.Kind,
            request.Payload.PlanNameId,
            name,
            audit,
            request.CancellationToken);
        if (updated is null)
        {
            return ProcessResponse<PlanNameDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Plan name not found",
                PlanNameErrorCodes.NotFound);
        }

        return ProcessResponse<PlanNameDto>.Succeeded(_planNameMapper.ToDto(updated));
    }
}
