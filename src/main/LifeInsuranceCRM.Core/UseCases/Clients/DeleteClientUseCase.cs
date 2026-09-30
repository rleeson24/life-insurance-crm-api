using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteClientUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteClientRequest> request);
}

public sealed class DeleteClientUseCase : IDeleteClientUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ISecurityAudit _securityAudit;

    public DeleteClientUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _securityAudit = securityAudit;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteClientRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _clientRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            await RecordDeleteAsync(request, success: false, SecurityAudit.StatusNotFound, "Client not found");
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        await RecordDeleteAsync(request, success: true, SecurityAudit.StatusNoContent, failureReason: null);
        return ProcessResponse<bool>.Succeeded(true);
    }

    private Task RecordDeleteAsync(
        ProcessRequest<DeleteClientRequest> request,
        bool success,
        int httpStatus,
        string? failureReason) =>
        _securityAudit.RecordAsync(
            AuthSecurityEventTypes.ClientDeleted,
            success,
            resource: "clients",
            request.CancellationToken,
            httpStatus,
            resultCount: success ? 1 : 0,
            targetId: request.Payload.ClientId,
            failureReason: failureReason);
}
