using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
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
    private readonly ISecurityAudit _securityAudit;

    public DeleteClientInteractionUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientInteractionRepository clientInteractionRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientInteractionRepository = clientInteractionRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _securityAudit = securityAudit;
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
            await RecordDeleteAsync(request, success: false, "Interaction not found");
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Interaction not found",
                ClientErrorCodes.InteractionNotFound);
        }

        await RecordDeleteAsync(request, success: true, failureReason: null);
        return ProcessResponse<bool>.Succeeded(true);
    }

    private Task RecordDeleteAsync(
        ProcessRequest<DeleteClientInteractionRequest> request,
        bool success,
        string? failureReason) =>
        _securityAudit.RecordAsync(
            AuthSecurityEventTypes.InteractionDeleted,
            success,
            resource: "interactions",
            request.CancellationToken,
            success ? SecurityAudit.StatusNoContent : SecurityAudit.StatusNotFound,
            resultCount: success ? 1 : 0,
            targetId: request.Payload.ClientInteractionId,
            detail: SecurityEventDetail.ClientId(request.Payload.ClientId),
            failureReason: failureReason);
}
