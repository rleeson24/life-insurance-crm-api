using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface ICreateSecondaryEnrollmentUseCase
{
    Task<ProcessResponse<SecondaryEnrollmentDto>> Execute(ProcessRequest<CreateSecondaryEnrollmentModel> request);
}

public sealed class CreateSecondaryEnrollmentUseCase : ICreateSecondaryEnrollmentUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly ISecondaryEnrollmentRepository _secondaryEnrollmentRepository;
    private readonly IClientMapper _clientMapper;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ISecondaryEnrollmentInputValidator _secondaryEnrollmentInputValidator;

    public CreateSecondaryEnrollmentUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        ISecondaryEnrollmentRepository secondaryEnrollmentRepository,
        IClientMapper clientMapper,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ISecondaryEnrollmentInputValidator secondaryEnrollmentInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _secondaryEnrollmentRepository = secondaryEnrollmentRepository;
        _clientMapper = clientMapper;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _secondaryEnrollmentInputValidator = secondaryEnrollmentInputValidator;
    }

    public async Task<ProcessResponse<SecondaryEnrollmentDto>> Execute(ProcessRequest<CreateSecondaryEnrollmentModel> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<SecondaryEnrollmentDto> failure))
        {
            return failure;
        }

        var client = await _clientRepository.GetByIdAsync(request.Payload.ClientId, request.CancellationToken);
        if (client is null)
        {
            return ProcessResponse<SecondaryEnrollmentDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        var inputValidation = _secondaryEnrollmentInputValidator.ValidateCreate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<SecondaryEnrollmentDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var enrollment = await _secondaryEnrollmentRepository.InsertAsync(
            request.Payload,
            _actorTracker.TenantId!.Value,
            audit,
            request.CancellationToken);

        return ProcessResponse<SecondaryEnrollmentDto>.Succeeded(_clientMapper.ToDto(enrollment));
    }
}
