using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IListSecondaryEnrollmentsUseCase
{
    Task<ProcessResponse<IReadOnlyList<SecondaryEnrollmentDto>>> Execute(ProcessRequest<ListSecondaryEnrollmentsRequest> request);
}

public sealed class ListSecondaryEnrollmentsUseCase : IListSecondaryEnrollmentsUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IClientRepository _clientRepository;
    private readonly ISecondaryEnrollmentRepository _secondaryEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListSecondaryEnrollmentsUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        ISecondaryEnrollmentRepository secondaryEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _secondaryEnrollmentRepository = secondaryEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<SecondaryEnrollmentDto>>> Execute(
        ProcessRequest<ListSecondaryEnrollmentsRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<IReadOnlyList<SecondaryEnrollmentDto>> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<IReadOnlyList<SecondaryEnrollmentDto>>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var enrollments = await _secondaryEnrollmentRepository.ListByClientIdAsync(
            request.Payload.ClientId,
            request.CancellationToken);

        var result = enrollments.Select(_clientMapper.ToDto).ToList();
        return ProcessResponse<IReadOnlyList<SecondaryEnrollmentDto>>.Succeeded(result);
    }
}
