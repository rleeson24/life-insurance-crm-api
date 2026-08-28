using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.PlanNames;

public interface IClonePlanNamesUseCase
{
    Task<ProcessResponse<ClonePlanNamesResultDto>> Execute(ProcessRequest<ClonePlanNamesModel> request);
}

public sealed class ClonePlanNamesUseCase : IClonePlanNamesUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IPlanNameRepository _planNameRepository;
    private readonly IPlanNameMapper _planNameMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IPlanNameInputValidator _planNameInputValidator;

    public ClonePlanNamesUseCase(
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

    public async Task<ProcessResponse<ClonePlanNamesResultDto>> Execute(
        ProcessRequest<ClonePlanNamesModel> request)
    {
        var validation = PlanNameUseCaseHelpers.ValidateAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<ClonePlanNamesResultDto> failure))
        {
            return failure;
        }

        var inputValidation = _planNameInputValidator.ValidateClone(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<ClonePlanNamesResultDto> inputFailure))
        {
            return inputFailure;
        }

        var sourceCount = await _planNameRepository.CountByYearAsync(
            request.Payload.Kind,
            request.Payload.SourceYear,
            request.CancellationToken);

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var cloned = await _planNameRepository.CloneYearAsync(
            request.Payload.Kind,
            _actorTracker.TenantId!.Value,
            request.Payload.SourceYear,
            request.Payload.TargetYear,
            audit,
            request.CancellationToken);

        return ProcessResponse<ClonePlanNamesResultDto>.Succeeded(new ClonePlanNamesResultDto
        {
            SourceCount = sourceCount,
            ClonedCount = cloned.Count,
            SkippedCount = sourceCount - cloned.Count,
            Items = cloned.Select(_planNameMapper.ToDto).ToList(),
        });
    }
}
