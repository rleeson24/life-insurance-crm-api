using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
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

    public DeleteMajorMedicalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IMajorMedicalEnrollmentRepository majorMedicalEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _majorMedicalEnrollmentRepository = majorMedicalEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
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
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Major Medical enrollment not found",
                ClientErrorCodes.MajorMedicalEnrollmentNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
