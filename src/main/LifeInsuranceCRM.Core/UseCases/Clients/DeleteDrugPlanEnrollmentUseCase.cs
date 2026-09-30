using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteDrugPlanEnrollmentUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteDrugPlanEnrollmentRequest> request);
}

public sealed class DeleteDrugPlanEnrollmentUseCase : IDeleteDrugPlanEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IDrugPlanEnrollmentRepository _drugPlanEnrollmentRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public DeleteDrugPlanEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteDrugPlanEnrollmentRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.DrugPlanEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Drug plan enrollment id is required",
                ClientErrorCodes.DrugPlanEnrollmentIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _drugPlanEnrollmentRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            request.Payload.DrugPlanEnrollmentId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            await RecordDeleteAsync(request, success: false, "Drug plan enrollment not found");
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Drug plan enrollment not found",
                ClientErrorCodes.DrugPlanEnrollmentNotFound);
        }

        await RecordDeleteAsync(request, success: true, failureReason: null);
        return ProcessResponse<bool>.Succeeded(true);
    }

    private Task RecordDeleteAsync(
        ProcessRequest<DeleteDrugPlanEnrollmentRequest> request,
        bool success,
        string? failureReason) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.EnrollmentDeleted,
            success,
            resource: "drug",
            request.CancellationToken,
            success ? SecurityAudit.StatusNoContent : SecurityAudit.StatusNotFound,
            resultCount: success ? 1 : 0,
            targetId: request.Payload.DrugPlanEnrollmentId,
            detail: SecurityEventDetail.ClientId(request.Payload.ClientId),
            failureReason: failureReason);
}
