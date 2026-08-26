using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.OrganizationUsers;

public interface IUpdateOrganizationUserUseCase
{
    Task<ProcessResponse<OrganizationUserDto>> Execute(ProcessRequest<UpdateOrganizationUserModel> request);
}

public sealed class UpdateOrganizationUserUseCase : IUpdateOrganizationUserUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IOrganizationUserInputValidator _organizationUserInputValidator;

    public UpdateOrganizationUserUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IOrganizationUserRepository organizationUserRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IOrganizationUserInputValidator organizationUserInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _organizationUserRepository = organizationUserRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _organizationUserInputValidator = organizationUserInputValidator;
    }

    public async Task<ProcessResponse<OrganizationUserDto>> Execute(
        ProcessRequest<UpdateOrganizationUserModel> request)
    {
        var validation = OrganizationUserUseCaseHelpers.ValidateUserManager(
            _actorTracker,
            _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<OrganizationUserDto> failure))
        {
            return failure;
        }

        var inputValidation = _organizationUserInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<OrganizationUserDto> inputFailure))
        {
            return inputFailure;
        }

        var existing = await _organizationUserRepository.GetByOrganizationUserIdAsync(
            request.Payload.OrganizationUserId,
            request.CancellationToken);
        if (!CanManageUser(existing))
        {
            return ProcessResponse<OrganizationUserDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Organization user not found",
                OrganizationUserErrorCodes.UserNotFound);
        }

        if (OrganizationRoles.IsSuperAdmin(request.Payload.Role)
            && !OrganizationRoles.IsSuperAdmin(existing!.Role))
        {
            return ProcessResponse<OrganizationUserDto>.InvalidRequestResponse(
                "SuperAdmin cannot be assigned in the app",
                OrganizationUserErrorCodes.RoleInvalid);
        }

        if (OrganizationRoles.IsSuperAdmin(existing!.Role)
            && !OrganizationRoles.IsSuperAdmin(request.Payload.Role))
        {
            return ProcessResponse<OrganizationUserDto>.InvalidRequestResponse(
                "SuperAdmin role cannot be changed in the app",
                OrganizationUserErrorCodes.SuperAdminRoleLocked);
        }

        var role = OrganizationRoles.IsSuperAdmin(existing.Role)
            ? OrganizationRoles.SuperAdmin
            : request.Payload.Role;

        var lastAdminBlocked = await IsDemotingLastAdminAsync(
            existing,
            role,
            request.Payload.IsActive,
            request.CancellationToken);
        if (lastAdminBlocked.IsFailed(out ProcessResponse<OrganizationUserDto> lastAdminFailure))
        {
            return lastAdminFailure;
        }

        var lastSuperAdminBlocked = await IsDeactivatingLastSuperAdminAsync(
            existing,
            request.Payload.IsActive,
            request.CancellationToken);
        if (lastSuperAdminBlocked.IsFailed(out ProcessResponse<OrganizationUserDto> lastSuperAdminFailure))
        {
            return lastSuperAdminFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var updated = await _organizationUserRepository.UpdateAsync(
            request.Payload.OrganizationUserId,
            TrimToNull(request.Payload.EmailAddress),
            request.Payload.DisplayName!.Trim(),
            role,
            request.Payload.IsActive,
            audit,
            request.CancellationToken);

        return updated is null
            ? ProcessResponse<OrganizationUserDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Organization user not found",
                OrganizationUserErrorCodes.UserNotFound)
            : ProcessResponse<OrganizationUserDto>.Succeeded(updated);
    }

    private bool CanManageUser(OrganizationUserDto? existing)
    {
        if (existing is null)
        {
            return false;
        }

        if (OrganizationRoles.IsSuperAdmin(_actorTracker.Role))
        {
            return true;
        }

        return existing.TenantId == _actorTracker.TenantId
            && !OrganizationRoles.IsSuperAdmin(existing.Role);
    }

    private async Task<ProcessResponse<bool>> IsDemotingLastAdminAsync(
        OrganizationUserDto existing,
        string nextRole,
        bool nextIsActive,
        CancellationToken cancellationToken)
    {
        var demotingLastAdmin =
            existing.IsActive
            && string.Equals(existing.Role, OrganizationRoles.Admin, StringComparison.Ordinal)
            && (!nextIsActive
                || !string.Equals(nextRole, OrganizationRoles.Admin, StringComparison.Ordinal));

        if (!demotingLastAdmin)
        {
            return ProcessResponse<bool>.Succeeded(true);
        }

        var adminCount = await _organizationUserRepository.CountActiveAdminsInTenantAsync(
            existing.TenantId,
            cancellationToken);
        return adminCount <= 1
            ? ProcessResponse<bool>.InvalidRequestResponse(
                "Cannot remove or demote the last active administrator",
                OrganizationUserErrorCodes.LastAdmin)
            : ProcessResponse<bool>.Succeeded(true);
    }

    private async Task<ProcessResponse<bool>> IsDeactivatingLastSuperAdminAsync(
        OrganizationUserDto existing,
        bool nextIsActive,
        CancellationToken cancellationToken)
    {
        var deactivatingLastSuperAdmin =
            existing.IsActive
            && OrganizationRoles.IsSuperAdmin(existing.Role)
            && !nextIsActive;

        if (!deactivatingLastSuperAdmin)
        {
            return ProcessResponse<bool>.Succeeded(true);
        }

        var superAdminCount = await _organizationUserRepository.CountActiveSuperAdminsAsync(cancellationToken);
        return superAdminCount <= 1
            ? ProcessResponse<bool>.InvalidRequestResponse(
                "Cannot deactivate the last SuperAdmin",
                OrganizationUserErrorCodes.LastSuperAdmin)
            : ProcessResponse<bool>.Succeeded(true);
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
