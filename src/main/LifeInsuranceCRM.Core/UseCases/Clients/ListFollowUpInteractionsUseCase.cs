using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListFollowUpInteractionsUseCase
{
    Task<ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>> Execute(ProcessRequest<ListFollowUpInteractionsRequest> request);
}

public sealed class ListFollowUpInteractionsUseCase : IListFollowUpInteractionsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ISecurityAudit _securityAudit;

    public ListFollowUpInteractionsUseCase(
        IActorTracker actorTracker,
        IClientInteractionRepository clientInteractionRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _clientInteractionRepository = clientInteractionRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _securityAudit = securityAudit;
    }

    public async Task<ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>> Execute(
        ProcessRequest<ListFollowUpInteractionsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<FollowUpInteractionDto>> failure))
        {
            return failure;
        }

        var interactions = await _clientInteractionRepository.ListFollowUpsAsync(request.CancellationToken);
        await _securityAudit.RecordAsync(
            AuthSecurityEventTypes.FollowUpsListed,
            success: true,
            resource: "follow-ups",
            cancellationToken: request.CancellationToken,
            httpStatus: SecurityAudit.StatusOk,
            resultCount: interactions.Count);
        return ProcessResponse<IReadOnlyList<FollowUpInteractionDto>>.Succeeded(interactions);
    }
}
