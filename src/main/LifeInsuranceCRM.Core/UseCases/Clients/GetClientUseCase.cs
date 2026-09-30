using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IGetClientUseCase
{
    Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<GetClientRequest> request);
}

public sealed class GetClientUseCase : IGetClientUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public GetClientUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<GetClientRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            await RecordViewAsync(request, success: false, resultCount: 0, SecurityAudit.StatusNotFound, "Client not found");
            return ProcessResponse<ClientDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        await RecordViewAsync(request, success: true, resultCount: 1, SecurityAudit.StatusOk, failureReason: null);
        return ProcessResponse<ClientDto>.Succeeded(_clientMapper.ToDto(client));
    }

    private Task RecordViewAsync(
        ProcessRequest<GetClientRequest> request,
        bool success,
        int resultCount,
        int httpStatus,
        string? failureReason) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.ClientViewed,
            success,
            resource: "clients",
            request.CancellationToken,
            httpStatus,
            resultCount,
            request.Payload.ClientId,
            failureReason: failureReason);
}
