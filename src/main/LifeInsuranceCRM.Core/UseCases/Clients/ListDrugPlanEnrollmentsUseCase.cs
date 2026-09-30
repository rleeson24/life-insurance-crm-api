using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListDrugPlanEnrollmentsUseCase
{
    Task<ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>>> Execute(ProcessRequest<ListDrugPlanEnrollmentsRequest> request);
}

public sealed class ListDrugPlanEnrollmentsUseCase : IListDrugPlanEnrollmentsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IDrugPlanEnrollmentRepository _drugPlanEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public ListDrugPlanEnrollmentsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>>> Execute(
        ProcessRequest<ListDrugPlanEnrollmentsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            await RecordListAsync(request, success: false, resultCount: 0, SecurityAudit.StatusNotFound, "Client not found");
            return ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var enrollments = await _drugPlanEnrollmentRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = enrollments.Select(_clientMapper.ToDto).ToList();
        await RecordListAsync(request, success: true, result.Count, SecurityAudit.StatusOk, failureReason: null);
        return ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>>.Succeeded(result);
    }

    private Task RecordListAsync(
        ProcessRequest<ListDrugPlanEnrollmentsRequest> request,
        bool success,
        int resultCount,
        int httpStatus,
        string? failureReason) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.EnrollmentListed,
            success,
            resource: "drug",
            request.CancellationToken,
            httpStatus,
            resultCount,
            request.Payload.ClientId,
            failureReason: failureReason);
}
