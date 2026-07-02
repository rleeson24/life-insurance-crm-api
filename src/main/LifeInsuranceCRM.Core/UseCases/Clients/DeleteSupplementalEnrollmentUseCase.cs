using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteSupplementalEnrollmentUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteSupplementalEnrollmentRequest> request);
}

public sealed class DeleteSupplementalEnrollmentUseCase : IDeleteSupplementalEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly ISupplementalEnrollmentRepository _supplementalEnrollmentRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public DeleteSupplementalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        ISupplementalEnrollmentRepository supplementalEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _supplementalEnrollmentRepository = supplementalEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteSupplementalEnrollmentRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.SupplementalEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Supplemental enrollment id is required",
                ClientErrorCodes.SupplementalEnrollmentIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _supplementalEnrollmentRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            request.Payload.SupplementalEnrollmentId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Supplemental enrollment not found",
                ClientErrorCodes.SupplementalEnrollmentNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
