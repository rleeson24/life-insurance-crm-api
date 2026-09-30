using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteMajorMedicalEnrollmentUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteMajorMedicalEnrollmentRequest> request);
}

public sealed class DeleteMajorMedicalEnrollmentUseCase : IDeleteMajorMedicalEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IMajorMedicalEnrollmentRepository _majorMedicalEnrollmentRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public DeleteMajorMedicalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IMajorMedicalEnrollmentRepository majorMedicalEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _majorMedicalEnrollmentRepository = majorMedicalEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteMajorMedicalEnrollmentRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.MajorMedicalEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Major Medical enrollment id is required",
                ClientErrorCodes.MajorMedicalEnrollmentIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _majorMedicalEnrollmentRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            request.Payload.MajorMedicalEnrollmentId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            await RecordDeleteAsync(request, success: false, "Major Medical enrollment not found");
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Major Medical enrollment not found",
                ClientErrorCodes.MajorMedicalEnrollmentNotFound);
        }

        await RecordDeleteAsync(request, success: true, failureReason: null);
        return ProcessResponse<bool>.Succeeded(true);
    }

    private Task RecordDeleteAsync(
        ProcessRequest<DeleteMajorMedicalEnrollmentRequest> request,
        bool success,
        string? failureReason) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.EnrollmentDeleted,
            success,
            resource: "major-medical",
            request.CancellationToken,
            success ? SecurityAudit.StatusNoContent : SecurityAudit.StatusNotFound,
            resultCount: success ? 1 : 0,
            targetId: request.Payload.MajorMedicalEnrollmentId,
            detail: SecurityEventDetail.ClientId(request.Payload.ClientId),
            failureReason: failureReason);
}
