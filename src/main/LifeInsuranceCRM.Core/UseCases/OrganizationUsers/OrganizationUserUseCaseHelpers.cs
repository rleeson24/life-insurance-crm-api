using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.OrganizationUsers;

internal static class OrganizationUserUseCaseHelpers
{
    public static ProcessResponse<bool> ValidateAdmin(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        var validation = clientUseCaseHelpers.ValidateActor(actorTracker);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (!string.Equals(actorTracker.Role, OrganizationRoles.Admin, StringComparison.Ordinal))
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Forbidden,
                "Administrator role is required",
                OrganizationUserErrorCodes.ActorNotAdmin);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
