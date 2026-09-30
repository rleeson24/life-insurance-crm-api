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

public interface IGetClientDetailUseCase
{
    Task<ProcessResponse<ClientDetailDto>> Execute(ProcessRequest<GetClientDetailRequest> request);
}

public sealed class GetClientDetailUseCase : IGetClientDetailUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IClientInteractionRepository _clientInteractionRepository;
    private readonly IMajorMedicalEnrollmentRepository _majorMedicalEnrollmentRepository;
    private readonly IDrugPlanEnrollmentRepository _drugPlanEnrollmentRepository;
    private readonly ISecondaryEnrollmentRepository _secondaryEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ISecurityAudit _securityAudit;

    public GetClientDetailUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientInteractionRepository clientInteractionRepository,
        IMajorMedicalEnrollmentRepository majorMedicalEnrollmentRepository,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        ISecondaryEnrollmentRepository secondaryEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientInteractionRepository = clientInteractionRepository;
        _majorMedicalEnrollmentRepository = majorMedicalEnrollmentRepository;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _secondaryEnrollmentRepository = secondaryEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _securityAudit = securityAudit;
    }

    public async Task<ProcessResponse<ClientDetailDto>> Execute(ProcessRequest<GetClientDetailRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<ClientDetailDto> failure))
        {
            return failure;
        }

        var clientId = request.Payload.ClientId;
        var cancellationToken = request.CancellationToken;

        var clientTask = _clientRepository.GetByIdAsync(clientId, cancellationToken);
        var interactionsTask = _clientInteractionRepository.ListByClientIdAsync(clientId, cancellationToken);
        var majorMedicalTask = _majorMedicalEnrollmentRepository.ListByClientIdAsync(clientId, cancellationToken);
        var drugPlanTask = _drugPlanEnrollmentRepository.ListByClientIdAsync(clientId, cancellationToken);
        var secondaryTask = _secondaryEnrollmentRepository.ListByClientIdAsync(clientId, cancellationToken);

        await Task.WhenAll(clientTask, interactionsTask, majorMedicalTask, drugPlanTask, secondaryTask);

        var client = await clientTask;
        if (client is null)
        {
            await _securityAudit.RecordAsync(
                AuthSecurityEventTypes.ClientDetailViewed,
                success: false,
                resource: "clients",
                request.CancellationToken,
                SecurityAudit.StatusNotFound,
                resultCount: 0,
                targetId: clientId,
                failureReason: "Client not found");
            return ProcessResponse<ClientDetailDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var detail = new ClientDetailDto
        {
            Client = _clientMapper.ToDto(client),
            Interactions = (await interactionsTask).Select(_clientMapper.ToDto).ToList(),
            MajorMedicalEnrollments = (await majorMedicalTask).Select(_clientMapper.ToDto).ToList(),
            DrugPlanEnrollments = (await drugPlanTask).Select(_clientMapper.ToDto).ToList(),
            SecondaryEnrollments = (await secondaryTask).Select(_clientMapper.ToDto).ToList(),
        };

        await _securityAudit.RecordAsync(
            AuthSecurityEventTypes.ClientDetailViewed,
            success: true,
            resource: "clients",
            request.CancellationToken,
            SecurityAudit.StatusOk,
            resultCount: 1,
            targetId: clientId,
            detail: SecurityEventDetail.ClientParts(
                detail.Interactions.Count,
                detail.MajorMedicalEnrollments.Count,
                detail.DrugPlanEnrollments.Count,
                detail.SecondaryEnrollments.Count));
        return ProcessResponse<ClientDetailDto>.Succeeded(detail);
    }
}
