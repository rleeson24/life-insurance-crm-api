using LifeInsuranceCRM.Core.Abstractions.Auth;
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
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public GetCurrentUserUseCase(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public Task<ProcessResponse<CurrentUserDto>> Execute(ProcessRequest<bool> request)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<CurrentUserDto> failure))
        {
            return Task.FromResult(failure);
        }

        return Task.FromResult(ProcessResponse<CurrentUserDto>.Succeeded(new CurrentUserDto
        {
            UserId = _actorTracker.UserId!.Value,
            Email = _actorTracker.UserEmail,
            TenantId = _actorTracker.TenantId!.Value,
            Role = _actorTracker.Role ?? string.Empty,
        }));
    }
}
