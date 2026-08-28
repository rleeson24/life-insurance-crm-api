using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.PlanNames;

public interface IListPlanNamesUseCase
{
    Task<ProcessResponse<IReadOnlyList<PlanNameDto>>> Execute(ProcessRequest<ListPlanNamesRequest> request);
}

public sealed class ListPlanNamesUseCase : IListPlanNamesUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IPlanNameRepository _planNameRepository;
    private readonly IPlanNameMapper _planNameMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IPlanNameInputValidator _planNameInputValidator;

    public ListPlanNamesUseCase(
        IActorTracker actorTracker,
        IPlanNameRepository planNameRepository,
        IPlanNameMapper planNameMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IPlanNameInputValidator planNameInputValidator)
    {
        _actorTracker = actorTracker;
        _planNameRepository = planNameRepository;
        _planNameMapper = planNameMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _planNameInputValidator = planNameInputValidator;
    }

    public async Task<ProcessResponse<IReadOnlyList<PlanNameDto>>> Execute(
        ProcessRequest<ListPlanNamesRequest> request)
    {
        var validation = PlanNameUseCaseHelpers.ValidateActor(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<PlanNameDto>> failure))
        {
            return failure;
        }

        var inputValidation = _planNameInputValidator.ValidateList(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<IReadOnlyList<PlanNameDto>> inputFailure))
        {
            return inputFailure;
        }

        var items = await _planNameRepository.ListByYearAsync(
            request.Payload.Kind,
            request.Payload.PlanYear,
            request.CancellationToken);

        return ProcessResponse<IReadOnlyList<PlanNameDto>>.Succeeded(
            items.Select(_planNameMapper.ToDto).ToList());
    }
}
