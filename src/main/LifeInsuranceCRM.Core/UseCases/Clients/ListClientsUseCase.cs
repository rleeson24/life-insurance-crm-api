using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListClientsUseCase
{
    Task<ProcessResponse<ListClientsResult>> Execute(ProcessRequest<ListClientsRequest> request);
}

public sealed class ListClientsUseCase : IListClientsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public ListClientsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<ListClientsResult>> Execute(ProcessRequest<ListClientsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<ListClientsResult> failure))
        {
            return failure;
        }

        var result = await _clientRepository.ListAsync(request.Payload, request.CancellationToken);
        await SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.ClientListed,
            success: true,
            resource: "clients",
            cancellationToken: request.CancellationToken,
            httpStatus: SecurityAudit.StatusOk,
            resultCount: result.Items.Count,
            detail: SecurityEventDetail.ListPage(result.Page, result.PageSize, result.TotalCount));
        return ProcessResponse<ListClientsResult>.Succeeded(result);
    }
}
