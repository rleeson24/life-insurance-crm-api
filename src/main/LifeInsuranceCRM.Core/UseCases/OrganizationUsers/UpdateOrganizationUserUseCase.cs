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
        var validation = OrganizationUserUseCaseHelpers.ValidateAdmin(
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
        if (existing is null || existing.TenantId != _actorTracker.TenantId)
        {
            return ProcessResponse<OrganizationUserDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Organization user not found",
                OrganizationUserErrorCodes.UserNotFound);
        }

        var demotingLastAdmin =
            existing.IsActive
            && string.Equals(existing.Role, OrganizationRoles.Admin, StringComparison.Ordinal)
            && (!request.Payload.IsActive
                || !string.Equals(request.Payload.Role, OrganizationRoles.Admin, StringComparison.Ordinal));

        if (demotingLastAdmin)
        {
            var adminCount = await _organizationUserRepository.CountActiveAdminsInTenantAsync(
                existing.TenantId,
                request.CancellationToken);
            if (adminCount <= 1)
            {
                return ProcessResponse<OrganizationUserDto>.InvalidRequestResponse(
                    "Cannot remove or demote the last active administrator",
                    OrganizationUserErrorCodes.LastAdmin);
            }
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var updated = await _organizationUserRepository.UpdateAsync(
            request.Payload.OrganizationUserId,
            TrimToNull(request.Payload.EmailAddress),
            request.Payload.DisplayName!.Trim(),
            request.Payload.Role,
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

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
