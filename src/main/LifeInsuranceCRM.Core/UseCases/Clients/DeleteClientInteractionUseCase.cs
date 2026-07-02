using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteClientInteractionUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteClientInteractionRequest> request);
}

public sealed class DeleteClientInteractionUseCase : IDeleteClientInteractionUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public DeleteClientInteractionUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientInteractionRepository clientInteractionRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientInteractionRepository = clientInteractionRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteClientInteractionRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.ClientInteractionId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Interaction id is required",
                ClientErrorCodes.InteractionIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _clientInteractionRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            request.Payload.ClientInteractionId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Interaction not found",
                ClientErrorCodes.InteractionNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
