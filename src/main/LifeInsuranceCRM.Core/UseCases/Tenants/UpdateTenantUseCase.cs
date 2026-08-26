using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Tenants;

public interface IUpdateTenantUseCase
{
    Task<ProcessResponse<TenantDto>> Execute(ProcessRequest<UpdateTenantModel> request);
}

public sealed class UpdateTenantUseCase : IUpdateTenantUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly ITenantRepository _tenantRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ITenantInputValidator _tenantInputValidator;

    public UpdateTenantUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        ITenantRepository tenantRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        ITenantInputValidator tenantInputValidator)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _tenantRepository = tenantRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _tenantInputValidator = tenantInputValidator;
    }

    public async Task<ProcessResponse<TenantDto>> Execute(ProcessRequest<UpdateTenantModel> request)
    {
        var validation = TenantUseCaseHelpers.ValidateSuperAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<TenantDto> failure))
        {
            return failure;
        }

        var inputValidation = _tenantInputValidator.ValidateUpdate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<TenantDto> inputFailure))
        {
            return inputFailure;
        }

        var existing = await _tenantRepository.GetByIdAsync(
            request.Payload.TenantId,
            request.CancellationToken);
        if (existing is null)
        {
            return ProcessResponse<TenantDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Organization not found",
                TenantErrorCodes.TenantNotFound);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var name = string.IsNullOrWhiteSpace(request.Payload.Name)
            ? null
            : request.Payload.Name.Trim();
        var updated = await _tenantRepository.UpdateAsync(
            request.Payload.TenantId,
            name,
            request.Payload.IsActive,
            audit,
            request.CancellationToken);

        return updated is null
            ? ProcessResponse<TenantDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Organization not found",
                TenantErrorCodes.TenantNotFound)
            : ProcessResponse<TenantDto>.Succeeded(updated);
    }
}
