using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface ICreateClientInteractionUseCase
{
    Task<ProcessResponse<ClientInteractionDto>> Execute(ProcessRequest<CreateClientInteractionModel> request);
}

public sealed class CreateClientInteractionUseCase : ICreateClientInteractionUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public CreateClientInteractionUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IClientInteractionRepository clientInteractionRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _clientInteractionRepository = clientInteractionRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<ClientInteractionDto>> Execute(ProcessRequest<CreateClientInteractionModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientInteractionDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<ClientInteractionDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var interaction = await _clientInteractionRepository.InsertAsync(
            request.Payload,
            _actorTracker.TenantId!.Value,
            audit,
            request.CancellationToken);

        return ProcessResponse<ClientInteractionDto>.Succeeded(_clientMapper.ToDto(interaction));
    }
}
