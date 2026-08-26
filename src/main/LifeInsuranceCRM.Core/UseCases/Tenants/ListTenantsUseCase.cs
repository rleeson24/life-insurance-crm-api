using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Tenants;

public interface IListTenantsUseCase
{
    Task<ProcessResponse<IReadOnlyList<TenantDto>>> Execute(ProcessRequest<bool> request);
}

public sealed class ListTenantsUseCase : IListTenantsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly ITenantRepository _tenantRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListTenantsUseCase(
        IActorTracker actorTracker,
        ITenantRepository tenantRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _tenantRepository = tenantRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<TenantDto>>> Execute(ProcessRequest<bool> request)
    {
        var validation = TenantUseCaseHelpers.ValidateSuperAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<TenantDto>> failure))
        {
            return failure;
        }

        var tenants = await _tenantRepository.ListAsync(request.CancellationToken);
        return ProcessResponse<IReadOnlyList<TenantDto>>.Succeeded(tenants);
    }
}
