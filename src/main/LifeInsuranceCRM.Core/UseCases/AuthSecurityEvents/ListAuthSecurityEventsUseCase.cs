using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Tenants;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;

public interface IListAuthSecurityEventsUseCase
{
    Task<ProcessResponse<ListAuthSecurityEventsResult>> Execute(
        ProcessRequest<ListAuthSecurityEventsRequest> request);
}

public sealed class ListAuthSecurityEventsUseCase : IListAuthSecurityEventsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IAuthSecurityEventRepository _authSecurityEventRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListAuthSecurityEventsUseCase(
        IActorTracker actorTracker,
        IAuthSecurityEventRepository authSecurityEventRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _authSecurityEventRepository = authSecurityEventRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<ListAuthSecurityEventsResult>> Execute(
        ProcessRequest<ListAuthSecurityEventsRequest> request)
    {
        var validation = TenantUseCaseHelpers.ValidateSuperAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<ListAuthSecurityEventsResult> failure))
        {
            return failure;
        }

        var result = await _authSecurityEventRepository.ListAsync(request.Payload, request.CancellationToken);
        return ProcessResponse<ListAuthSecurityEventsResult>.Succeeded(result);
    }
}
