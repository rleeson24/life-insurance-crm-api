using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
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
    private readonly ISecurityAudit _securityAudit;

    public ListClientInteractionsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientInteractionRepository clientInteractionRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientInteractionRepository = clientInteractionRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _securityAudit = securityAudit;
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
            await RecordListAsync(request, success: false, resultCount: 0, SecurityAudit.StatusNotFound, "Client not found");
            return ProcessResponse<IReadOnlyList<ClientInteractionDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var interactions = await _clientInteractionRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = interactions.Select(_clientMapper.ToDto).ToList();
        await RecordListAsync(request, success: true, result.Count, SecurityAudit.StatusOk, failureReason: null);
        return ProcessResponse<IReadOnlyList<ClientInteractionDto>>.Succeeded(result);
    }

    private Task RecordListAsync(
        ProcessRequest<ListClientInteractionsRequest> request,
        bool success,
        int resultCount,
        int httpStatus,
        string? failureReason) =>
        _securityAudit.RecordAsync(
            AuthSecurityEventTypes.InteractionListed,
            success,
            resource: "interactions",
            request.CancellationToken,
            httpStatus,
            resultCount,
            request.Payload.ClientId,
            failureReason: failureReason);
}
