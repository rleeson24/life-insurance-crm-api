using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IClientUseCaseHelpers
{
    ProcessResponse<bool> ValidateActor(IActorTracker actorTracker);

    ProcessResponse<bool> ValidateClientAccess(IActorTracker actorTracker, Guid clientId);

    AuditStamp CreateAuditStamp(IActorTracker actorTracker, INowProvider nowProvider);

    ProcessResponse<bool> ValidateClientId(Guid clientId);
}

public sealed class ClientUseCaseHelpers : IClientUseCaseHelpers
{
    public ProcessResponse<bool> ValidateActor(IActorTracker actorTracker)
    {
        if (!actorTracker.IsAuthenticated || actorTracker.UserId is null || actorTracker.TenantId is null)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Unauthorized,
                "Authentication required",
                ClientErrorCodes.ActorNotAuthenticated);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    public ProcessResponse<bool> ValidateClientAccess(IActorTracker actorTracker, Guid clientId)
    {
        var authValidation = ValidateActor(actorTracker);
        if (!authValidation.IsSuccess)
        {
            return authValidation;
        }

        return ValidateClientId(clientId);
    }

    public AuditStamp CreateAuditStamp(IActorTracker actorTracker, INowProvider nowProvider) =>
        new(actorTracker.UserId!.Value, nowProvider.UtcNow);

    public ProcessResponse<bool> ValidateClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Client id is required",
                ClientErrorCodes.ClientIdInvalid);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
