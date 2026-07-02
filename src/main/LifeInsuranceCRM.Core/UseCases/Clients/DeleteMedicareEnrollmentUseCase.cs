using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteMedicareEnrollmentUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteMedicareEnrollmentRequest> request);
}

public sealed class DeleteMedicareEnrollmentUseCase : IDeleteMedicareEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IMedicareEnrollmentRepository _medicareEnrollmentRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public DeleteMedicareEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IMedicareEnrollmentRepository medicareEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _medicareEnrollmentRepository = medicareEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteMedicareEnrollmentRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload.MedicareEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Medicare enrollment id is required",
                ClientErrorCodes.MedicareEnrollmentIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _medicareEnrollmentRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            request.Payload.MedicareEnrollmentId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Medicare enrollment not found",
                ClientErrorCodes.MedicareEnrollmentNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
