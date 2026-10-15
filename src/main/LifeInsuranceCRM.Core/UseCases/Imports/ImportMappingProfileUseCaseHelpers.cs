using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

internal static class ImportMappingProfileUseCaseHelpers
{
    public static ProcessResponse<bool> ValidateAdmin(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        var validation = clientUseCaseHelpers.ValidateActor(actorTracker);
        if (!validation.IsSuccess)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Unauthorized,
                "Authentication required",
                ImportErrorCodes.ActorNotAuthenticated);
        }

        if (!OrganizationRoles.CanManageOrganizationUsers(actorTracker.Role))
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Forbidden,
                "Administrator role is required",
                ImportErrorCodes.ActorNotAdmin);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
