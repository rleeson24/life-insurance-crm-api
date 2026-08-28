using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
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

    public DeleteDrugPlanEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
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
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Drug plan enrollment not found",
                ClientErrorCodes.DrugPlanEnrollmentNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
