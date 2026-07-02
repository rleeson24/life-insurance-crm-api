using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
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
    private readonly IMedicareEnrollmentRepository _medicareEnrollmentRepository;
    private readonly ISupplementalEnrollmentRepository _supplementalEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public GetClientDetailUseCase(
        IActorTracker actorTracker,
        IClientRepository clientRepository,
        IClientInteractionRepository clientInteractionRepository,
        IMedicareEnrollmentRepository medicareEnrollmentRepository,
        ISupplementalEnrollmentRepository supplementalEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _clientRepository = clientRepository;
        _clientInteractionRepository = clientInteractionRepository;
        _medicareEnrollmentRepository = medicareEnrollmentRepository;
        _supplementalEnrollmentRepository = supplementalEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
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
        var medicareTask = _medicareEnrollmentRepository.ListByClientIdAsync(clientId, cancellationToken);
        var supplementalTask = _supplementalEnrollmentRepository.ListByClientIdAsync(clientId, cancellationToken);

        await Task.WhenAll(clientTask, interactionsTask, medicareTask, supplementalTask);

        var client = await clientTask;
        if (client is null)
        {
            return ProcessResponse<ClientDetailDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var detail = new ClientDetailDto
        {
            Client = _clientMapper.ToDto(client),
            Interactions = (await interactionsTask).Select(_clientMapper.ToDto).ToList(),
            MedicareEnrollments = (await medicareTask).Select(_clientMapper.ToDto).ToList(),
            SupplementalEnrollments = (await supplementalTask).Select(_clientMapper.ToDto).ToList(),
        };

        return ProcessResponse<ClientDetailDto>.Succeeded(detail);
    }
}
