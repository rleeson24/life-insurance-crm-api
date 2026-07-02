using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListClientInteractionsUseCase
{
    Task<ProcessResponse<IReadOnlyList<ClientInteractionDto>>> Execute(ProcessRequest<ListClientInteractionsRequest> request);
}

public sealed class ListClientInteractionsUseCase : IListClientInteractionsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListClientInteractionsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientInteractionRepository clientInteractionRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientInteractionRepository = clientInteractionRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<ClientInteractionDto>>> Execute(
        ProcessRequest<ListClientInteractionsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<ClientInteractionDto>> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<IReadOnlyList<ClientInteractionDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var interactions = await _clientInteractionRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = interactions.Select(_clientMapper.ToDto).ToList();
        return ProcessResponse<IReadOnlyList<ClientInteractionDto>>.Succeeded(result);
    }
}
