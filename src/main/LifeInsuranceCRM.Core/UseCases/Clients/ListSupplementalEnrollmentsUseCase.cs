using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListSupplementalEnrollmentsUseCase
{
    Task<ProcessResponse<IReadOnlyList<SupplementalEnrollmentDto>>> Execute(ProcessRequest<ListSupplementalEnrollmentsRequest> request);
}

public sealed class ListSupplementalEnrollmentsUseCase : IListSupplementalEnrollmentsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly ISupplementalEnrollmentRepository _supplementalEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListSupplementalEnrollmentsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        ISupplementalEnrollmentRepository supplementalEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _supplementalEnrollmentRepository = supplementalEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<SupplementalEnrollmentDto>>> Execute(
        ProcessRequest<ListSupplementalEnrollmentsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<SupplementalEnrollmentDto>> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<IReadOnlyList<SupplementalEnrollmentDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var enrollments = await _supplementalEnrollmentRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = enrollments.Select(_clientMapper.ToDto).ToList();
        return ProcessResponse<IReadOnlyList<SupplementalEnrollmentDto>>.Succeeded(result);
    }
}
