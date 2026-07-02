using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IUpdateClientUseCase
{
    Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<UpdateClientModel> request);
}

public sealed class UpdateClientUseCase : IUpdateClientUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public UpdateClientUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<UpdateClientModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientDto> failure))
        {
            return failure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var client = await _clientRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
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
