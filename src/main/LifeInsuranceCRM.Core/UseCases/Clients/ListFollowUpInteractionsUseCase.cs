using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListFollowUpInteractionsUseCase
{
    Task<ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>> Execute(ProcessRequest<ListFollowUpInteractionsRequest> request);
}

public sealed class ListFollowUpInteractionsUseCase : IListFollowUpInteractionsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListFollowUpInteractionsUseCase(
        IActorTracker actorTracker,
        IClientInteractionRepository clientInteractionRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientInteractionRepository = clientInteractionRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>> Execute(
        ProcessRequest<ListFollowUpInteractionsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<FollowUpInteractionDto>> failure))
        {
            return failure;
        }

        var interactions = await _clientInteractionRepository.ListFollowUpsAsync(request.CancellationToken);
        return ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>.Succeeded(interactions);
    }
}
