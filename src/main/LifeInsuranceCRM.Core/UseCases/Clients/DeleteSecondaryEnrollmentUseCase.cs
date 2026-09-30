using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteSecondaryEnrollmentUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteSecondaryEnrollmentRequest> request);
}

public sealed class DeleteSecondaryEnrollmentUseCase : IDeleteSecondaryEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly ISecondaryEnrollmentRepository _secondaryEnrollmentRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public DeleteSecondaryEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        ISecondaryEnrollmentRepository secondaryEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _secondaryEnrollmentRepository = secondaryEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteSecondaryEnrollmentRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.SecondaryEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Secondary enrollment id is required",
                ClientErrorCodes.SecondaryEnrollmentIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _secondaryEnrollmentRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            request.Payload.SecondaryEnrollmentId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            await RecordDeleteAsync(request, success: false, "Secondary enrollment not found");
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Secondary enrollment not found",
                ClientErrorCodes.SecondaryEnrollmentNotFound);
        }

        await RecordDeleteAsync(request, success: true, failureReason: null);
        return ProcessResponse<bool>.Succeeded(true);
    }

    private Task RecordDeleteAsync(
        ProcessRequest<DeleteSecondaryEnrollmentRequest> request,
        bool success,
        string? failureReason) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.EnrollmentDeleted,
            success,
            resource: "secondary",
            request.CancellationToken,
            success ? SecurityAudit.StatusNoContent : SecurityAudit.StatusNotFound,
            resultCount: success ? 1 : 0,
            targetId: request.Payload.SecondaryEnrollmentId,
            detail: SecurityEventDetail.ClientId(request.Payload.ClientId),
            failureReason: failureReason);
}
