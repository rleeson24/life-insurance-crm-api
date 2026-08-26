using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.OrganizationUsers;

public interface ICreateOrganizationUserUseCase
{
    Task<ProcessResponse<OrganizationUserDto>> Execute(ProcessRequest<CreateOrganizationUserModel> request);
}

public sealed class CreateOrganizationUserUseCase : ICreateOrganizationUserUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly IOrganizationUserInputValidator _organizationUserInputValidator;

    public CreateOrganizationUserUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IOrganizationUserRepository organizationUserRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IOrganizationUserInputValidator organizationUserInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _organizationUserRepository = organizationUserRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _organizationUserInputValidator = organizationUserInputValidator;
    }

    public async Task<ProcessResponse<OrganizationUserDto>> Execute(
        ProcessRequest<CreateOrganizationUserModel> request)
    {
        var validation = OrganizationUserUseCaseHelpers.ValidateAdmin(
            _actorTracker,
            _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<OrganizationUserDto> failure))
        {
            return failure;
        }

        var inputValidation = _organizationUserInputValidator.ValidateCreate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<OrganizationUserDto> inputFailure))
        {
            return inputFailure;
        }

        if (await _organizationUserRepository.ExistsByUserIdAsync(
                request.Payload.UserId,
                request.CancellationToken))
        {
            return ProcessResponse<OrganizationUserDto>.WithStatus(
                UseCaseStatus.Conflict,
                "That Entra user is already mapped to a CRM organization",
                OrganizationUserErrorCodes.UserAlreadyExists);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var tenantId = _actorTracker.TenantId!.Value;

        if (request.Payload.CreateNewTenant)
        {
            tenantId = Guid.NewGuid();
            await _organizationUserRepository.InsertTenantAsync(
                tenantId,
                request.Payload.NewTenantName!.Trim(),
                audit,
                request.CancellationToken);
        }

        var created = await _organizationUserRepository.InsertAsync(
            tenantId,
            request.Payload.UserId,
            TrimToNull(request.Payload.EmailAddress),
            request.Payload.DisplayName!.Trim(),
            request.Payload.Role,
            audit,
            request.CancellationToken);

        return ProcessResponse<OrganizationUserDto>.Succeeded(created);
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
