using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListClientsUseCase
{
    Task<ProcessResponse<ListClientsResult>> Execute(ProcessRequest<ListClientsRequest> request);
}

public sealed class ListClientsUseCase : IListClientsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListClientsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<ListClientsResult>> Execute(ProcessRequest<ListClientsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<ListClientsResult> failure))
        {
            return failure;
        }

        var result = await _clientRepository.ListAsync(request.Payload, request.CancellationToken);
        return ProcessResponse<ListClientsResult>.Succeeded(result);
    }
}
