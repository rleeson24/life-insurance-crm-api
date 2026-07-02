using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IUpdateMedicareEnrollmentUseCase
{
    Task<ProcessResponse<MedicareEnrollmentDto>> Execute(ProcessRequest<UpdateMedicareEnrollmentModel> request);
}

public sealed class UpdateMedicareEnrollmentUseCase : IUpdateMedicareEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IMedicareEnrollmentRepository _medicareEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public UpdateMedicareEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IMedicareEnrollmentRepository medicareEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _medicareEnrollmentRepository = medicareEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<MedicareEnrollmentDto>> Execute(ProcessRequest<UpdateMedicareEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<MedicareEnrollmentDto> failure))
        {
            return failure;
        }

        if (request.Payload.MedicareEnrollmentId == Guid.Empty)
        {
            return ProcessResponse<MedicareEnrollmentDto>.InvalidRequestResponse(
                "Medicare enrollment id is required",
                ClientErrorCodes.MedicareEnrollmentIdInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _medicareEnrollmentRepository.UpdateAsync(request.Payload, audit, request.CancellationToken);
        if (enrollment is null)
        {
            return ProcessResponse<MedicareEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Medicare enrollment not found",
                ClientErrorCodes.MedicareEnrollmentNotFound);
        }

        return ProcessResponse<MedicareEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
