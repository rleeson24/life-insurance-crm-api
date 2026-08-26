using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Tenants;

public interface ICreateTenantUseCase
{
    Task<ProcessResponse<TenantDto>> Execute(ProcessRequest<CreateTenantModel> request);
}

public sealed class CreateTenantUseCase : ICreateTenantUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly ITenantRepository _tenantRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;
    private readonly ITenantInputValidator _tenantInputValidator;

    public CreateTenantUseCase(
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

    public async Task<ProcessResponse<TenantDto>> Execute(ProcessRequest<CreateTenantModel> request)
    {
        var validation = TenantUseCaseHelpers.ValidateSuperAdmin(_actorTracker, _clientUseCaseHelpers);
        if (validation.IsFailed(out ProcessResponse<TenantDto> failure))
        {
            return failure;
        }

        var inputValidation = _tenantInputValidator.ValidateCreate(request.Payload);
        if (inputValidation.IsFailed(out ProcessResponse<TenantDto> inputFailure))
        {
            return inputFailure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var created = await _tenantRepository.InsertAsync(
            request.Payload.Name!.Trim(),
            audit,
            request.CancellationToken);

        return ProcessResponse<TenantDto>.Succeeded(created);
    }
}
