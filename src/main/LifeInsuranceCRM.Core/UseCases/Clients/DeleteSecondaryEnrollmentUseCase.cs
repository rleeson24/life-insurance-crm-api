using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
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

    public DeleteSecondaryEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        ISecondaryEnrollmentRepository secondaryEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _secondaryEnrollmentRepository = secondaryEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
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
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Secondary enrollment not found",
                ClientErrorCodes.SecondaryEnrollmentNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
