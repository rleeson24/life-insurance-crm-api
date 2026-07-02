using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface ICreateSupplementalEnrollmentUseCase
{
    Task<ProcessResponse<SupplementalEnrollmentDto>> Execute(ProcessRequest<CreateSupplementalEnrollmentModel> request);
}

public sealed class CreateSupplementalEnrollmentUseCase : ICreateSupplementalEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly ISupplementalEnrollmentRepository _supplementalEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public CreateSupplementalEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        ISupplementalEnrollmentRepository supplementalEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _supplementalEnrollmentRepository = supplementalEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<SupplementalEnrollmentDto>> Execute(ProcessRequest<CreateSupplementalEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<SupplementalEnrollmentDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<SupplementalEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _supplementalEnrollmentRepository.InsertAsync(
            request.Payload,
            _actorTracker.TenantId!.Value,
            audit,
            request.CancellationToken);

        return ProcessResponse<SupplementalEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
