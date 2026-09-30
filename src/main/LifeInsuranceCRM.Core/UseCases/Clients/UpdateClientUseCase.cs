using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IUpdateClientUseCase
{
    Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<UpdateClientModel> request);
}

public sealed class UpdateClientUseCase : IUpdateClientUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IClientInputValidator _clientInputValidator;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public UpdateClientUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IClientInputValidator clientInputValidator,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _clientInputValidator = clientInputValidator;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<ClientDto>> Execute(ProcessRequest<UpdateClientModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientDto> failure))
        {
            return failure;
        }

        var inputValidation = _clientInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<ClientDto> inputFailure))
        {
            return inputFailure;
        }

        var before = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var client = await _clientRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (client is null)
        {
            await RecordUpdateAsync(request, success: false, SecurityAudit.StatusNotFound, detail: null, "Client not found");
            return ProcessResponse<ClientDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        await RecordUpdateAsync(
            request,
            success: true,
            SecurityAudit.StatusOk,
            SecurityEventDetail.ClientUpdate(before, request.Payload),
            failureReason: null);
        return ProcessResponse<ClientDto>.Succeeded(_clientMapper.ToDto(client));
    }

    private Task RecordUpdateAsync(
        ProcessRequest<UpdateClientModel> request,
        bool success,
        int httpStatus,
        string? detail,
        string? failureReason) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.ClientUpdated,
            success,
            resource: "clients",
            request.CancellationToken,
            httpStatus,
            resultCount: success ? 1 : 0,
            targetId: request.Payload.ClientId,
            detail: detail,
            failureReason: failureReason);
}
