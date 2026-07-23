using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IUpdateClientInteractionUseCase
{
    Task<ProcessResponse<ClientInteractionDto>> Execute(ProcessRequest<UpdateClientInteractionModel> request);
}

public sealed class UpdateClientInteractionUseCase : IUpdateClientInteractionUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IClientInteractionInputValidator _clientInteractionInputValidator;

    public UpdateClientInteractionUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientInteractionRepository clientInteractionRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IClientInteractionInputValidator clientInteractionInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientInteractionRepository = clientInteractionRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _clientInteractionInputValidator = clientInteractionInputValidator;
    }

    public async Task<ProcessResponse<ClientInteractionDto>> Execute(ProcessRequest<UpdateClientInteractionModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientInteractionDto> failure))
        {
            return failure;
        }

        if (request.Payload.ClientInteractionId == Guid.Empty)
        {
            return ProcessResponse<ClientInteractionDto>.InvalidRequestResponse(
                "Interaction id is required",
                ClientErrorCodes.InteractionIdInvalid);
        }

        var inputValidation = _clientInteractionInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<ClientInteractionDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var interaction = await _clientInteractionRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (interaction is null)
        {
            return ProcessResponse<ClientInteractionDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Interaction not found",
                ClientErrorCodes.InteractionNotFound);
        }

        return ProcessResponse<ClientInteractionDto>.Succeeded(_clientMapper.ToDto(interaction));
    }
}
