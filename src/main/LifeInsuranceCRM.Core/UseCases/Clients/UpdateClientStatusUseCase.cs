using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IUpdateClientStatusUseCase
{
    Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<UpdateClientStatusModel> request);
}

public sealed class UpdateClientStatusUseCase : IUpdateClientStatusUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public UpdateClientStatusUseCase(
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

    public async Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<UpdateClientStatusModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientDto> failure))
        {
            return failure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var client = await _clientRepository.UpdateStatusAsync(request.Payload, audit, request.CancellationToken);
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
