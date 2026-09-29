using System.Text.RegularExpressions;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;

public interface IRecordAuthSessionEventUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<RecordAuthSessionEventRequest> request);
}

public sealed partial class RecordAuthSessionEventUseCase : IRecordAuthSessionEventUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public RecordAuthSessionEventUseCase(
        IActorTracker actorTracker,
        IAuthSecurityEventRecorder authSecurityEventRecorder,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _authSecurityEventRecorder = authSecurityEventRecorder;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<RecordAuthSessionEventRequest> request)
    {
        var eventType = request.Payload.EventType;
        if (eventType == AuthSecurityEventTypes.LoginFailed)
        {
            await _authSecurityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.LoginFailed,
                success: false,
                failureReason: SanitizeFailureReason(request.Payload.FailureReason),
                cancellationToken: request.CancellationToken);
            return ProcessResponse<bool>.Succeeded(true);
        }

        var validation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (eventType is not AuthSecurityEventTypes.LoginSucceeded and not AuthSecurityEventTypes.Logout)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Auth session event type is not supported",
                AuthSecurityEventErrorCodes.EventTypeInvalid);
        }

        await _authSecurityEventRecorder.RecordAsync(
            eventType,
            success: true,
            cancellationToken: request.CancellationToken);
        return ProcessResponse<bool>.Succeeded(true);
    }

    private static string SanitizeFailureReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "login_failed";
        }

        var trimmed = reason.Trim();
        return FailureReasonPattern().IsMatch(trimmed) ? trimmed : "login_failed";
    }

    [GeneratedRegex("^[A-Za-z0-9_]{1,64}$")]
    private static partial Regex FailureReasonPattern();
}
