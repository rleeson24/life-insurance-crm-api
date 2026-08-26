using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.OrganizationUsers;

public interface IListOrganizationUsersUseCase
{
    Task<ProcessResponse<IReadOnlyList<OrganizationUserDto>>> Execute(
        ProcessRequest<ListOrganizationUsersRequest> request);
}

public sealed class ListOrganizationUsersUseCase : IListOrganizationUsersUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListOrganizationUsersUseCase(
        IActorTracker actorTracker,
        IOrganizationUserRepository organizationUserRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _organizationUserRepository = organizationUserRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<OrganizationUserDto>>> Execute(
        ProcessRequest<ListOrganizationUsersRequest> request)
    {
        var validation = OrganizationUserUseCaseHelpers.ValidateUserManager(
            _actorTracker,
            _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<OrganizationUserDto>> failure))
        {
            return failure;
        }

        var isSuperAdmin = OrganizationRoles.IsSuperAdmin(_actorTracker.Role);
        var tenantId = isSuperAdmin
            ? request.Payload.TenantId
            : _actorTracker.TenantId;
        var users = await _organizationUserRepository.ListAsync(
            tenantId,
            includeSuperAdmins: isSuperAdmin,
            request.CancellationToken);

        return ProcessResponse<IReadOnlyList<OrganizationUserDto>>.Succeeded(users);
    }
}
