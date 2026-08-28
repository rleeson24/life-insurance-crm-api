using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.PlanNames;

internal static class PlanNameUseCaseHelpers
{
    public static ProcessResponse<bool> ValidateActor(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        var validation = clientUseCaseHelpers.ValidateActor(actorTracker);
        if (!validation.IsSuccess)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Unauthorized,
                "Authentication required",
                PlanNameErrorCodes.ActorNotAuthenticated);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    public static ProcessResponse<bool> ValidateAdmin(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        var validation = ValidateActor(actorTracker, clientUseCaseHelpers);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (!OrganizationRoles.CanManageOrganizationUsers(actorTracker.Role))
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Forbidden,
                "Administrator role is required",
                PlanNameErrorCodes.ActorNotAdmin);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
