using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.OrganizationUsers;

public interface IGetCurrentUserUseCase
{
    Task<ProcessResponse<CurrentUserDto>> Execute(ProcessRequest<bool> request);
}

public sealed class GetCurrentUserUseCase : IGetCurrentUserUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly ITenantRepository _tenantRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public GetCurrentUserUseCase(
        IActorTracker actorTracker,
        ITenantRepository tenantRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _tenantRepository = tenantRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<CurrentUserDto>> Execute(ProcessRequest<bool> request)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<CurrentUserDto> failure))
        {
            return failure;
        }

        var tenant = await _tenantRepository.GetByIdAsync(
            _actorTracker.TenantId!.Value,
            request.CancellationToken);

        return ProcessResponse<CurrentUserDto>.Succeeded(new CurrentUserDto
        {
            UserId = _actorTracker.UserId!.Value,
            Email = _actorTracker.UserEmail,
            TenantId = _actorTracker.TenantId.Value,
            TenantName = tenant?.Name,
            Role = _actorTracker.Role ?? string.Empty,
        });
    }
}
