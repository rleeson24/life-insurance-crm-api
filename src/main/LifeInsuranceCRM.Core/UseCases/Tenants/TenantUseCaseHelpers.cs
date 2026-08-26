using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Tenants;

internal static class TenantUseCaseHelpers
{
    public static ProcessResponse<bool> ValidateSuperAdmin(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        var validation = clientUseCaseHelpers.ValidateActor(actorTracker);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (!OrganizationRoles.IsSuperAdmin(actorTracker.Role))
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Forbidden,
                "SuperAdmin role is required",
                TenantErrorCodes.ActorNotSuperAdmin);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
