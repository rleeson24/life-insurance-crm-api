using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IGetClientUseCase
{
    Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<GetClientRequest> request);
}

public sealed class GetClientUseCase : IGetClientUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public GetClientUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<GetClientRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<ClientDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        return ProcessResponse<ClientDto>.Succeeded(_clientMapper.ToDto(client));
    }
}
