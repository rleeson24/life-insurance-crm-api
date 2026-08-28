using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
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

    public ListDrugPlanEnrollmentsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IDrugPlanEnrollmentRepository drugPlanEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _drugPlanEnrollmentRepository = drugPlanEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
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
            return ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var enrollments = await _drugPlanEnrollmentRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = enrollments.Select(_clientMapper.ToDto).ToList();
        return ProcessResponse<IReadOnlyList<DrugPlanEnrollmentDto>>.Succeeded(result);
    }
}
