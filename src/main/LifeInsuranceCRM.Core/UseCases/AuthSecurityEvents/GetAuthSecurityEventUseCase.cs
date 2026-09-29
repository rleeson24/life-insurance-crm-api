using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.UseCases.Tenants;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;

public interface IGetAuthSecurityEventUseCase
{
    Task<ProcessResponse<AuthSecurityEventDto>> Execute(ProcessRequest<GetAuthSecurityEventRequest> request);
}

public sealed class GetAuthSecurityEventUseCase : IGetAuthSecurityEventUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IAuthSecurityEventRepository _authSecurityEventRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public GetAuthSecurityEventUseCase(
        IActorTracker actorTracker,
        IAuthSecurityEventRepository authSecurityEventRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _authSecurityEventRepository = authSecurityEventRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<AuthSecurityEventDto>> Execute(
        ProcessRequest<GetAuthSecurityEventRequest> request)
    {
        var validation = TenantUseCaseHelpers.ValidateSuperAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<AuthSecurityEventDto> failure))
        {
            return failure;
        }

        if (request.Payload.AuthSecurityEventId == Guid.Empty)
        {
            return ProcessResponse<AuthSecurityEventDto>.InvalidRequestResponse(
                "Security event id is required",
                AuthSecurityEventErrorCodes.EventIdInvalid);
        }

        var securityEvent = await _authSecurityEventRepository.GetByIdAsync(
            request.Payload.AuthSecurityEventId,
            request.CancellationToken);
        if (securityEvent is null)
        {
            return ProcessResponse<AuthSecurityEventDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Security event not found",
                AuthSecurityEventErrorCodes.EventNotFound);
        }

        return ProcessResponse<AuthSecurityEventDto>.Succeeded(securityEvent);
    }
}
