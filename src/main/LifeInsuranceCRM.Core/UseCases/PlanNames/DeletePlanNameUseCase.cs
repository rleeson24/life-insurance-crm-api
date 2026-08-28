using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.PlanNames;

public interface IDeletePlanNameUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeletePlanNameRequest> request);
}

public sealed class DeletePlanNameUseCase : IDeletePlanNameUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IPlanNameRepository _planNameRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public DeletePlanNameUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IPlanNameRepository planNameRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _planNameRepository = planNameRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeletePlanNameRequest> request)
    {
        var validation = PlanNameUseCaseHelpers.ValidateAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.PlanNameId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Plan name id is required",
                PlanNameErrorCodes.IdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _planNameRepository.SoftDeleteAsync(
            request.Payload.Kind,
            request.Payload.PlanNameId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Plan name not found",
                PlanNameErrorCodes.NotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
