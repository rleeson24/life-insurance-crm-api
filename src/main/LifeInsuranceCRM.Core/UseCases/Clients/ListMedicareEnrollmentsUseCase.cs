using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListMedicareEnrollmentsUseCase
{
    Task<ProcessResponse<IReadOnlyList<MedicareEnrollmentDto>>> Execute(ProcessRequest<ListMedicareEnrollmentsRequest> request);
}

public sealed class ListMedicareEnrollmentsUseCase : IListMedicareEnrollmentsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly IMedicareEnrollmentRepository _medicareEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListMedicareEnrollmentsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IMedicareEnrollmentRepository medicareEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _medicareEnrollmentRepository = medicareEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<MedicareEnrollmentDto>>> Execute(
        ProcessRequest<ListMedicareEnrollmentsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<MedicareEnrollmentDto>> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<IReadOnlyList<MedicareEnrollmentDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var enrollments = await _medicareEnrollmentRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = enrollments.Select(_clientMapper.ToDto).ToList();
        return ProcessResponse<IReadOnlyList<MedicareEnrollmentDto>>.Succeeded(result);
    }
}
