using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface ICreateMedicareEnrollmentUseCase
{
    Task<ProcessResponse<MedicareEnrollmentDto>> Execute(ProcessRequest<CreateMedicareEnrollmentModel> request);
}

public sealed class CreateMedicareEnrollmentUseCase : ICreateMedicareEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IMedicareEnrollmentRepository _medicareEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public CreateMedicareEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IMedicareEnrollmentRepository medicareEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _medicareEnrollmentRepository = medicareEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<MedicareEnrollmentDto>> Execute(ProcessRequest<CreateMedicareEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<MedicareEnrollmentDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<MedicareEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _medicareEnrollmentRepository.InsertAsync(
            request.Payload,
            _actorTracker.TenantId!.Value,
            audit,
            request.CancellationToken);

        return ProcessResponse<MedicareEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
