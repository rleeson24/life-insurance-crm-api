using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

public interface IImportAccessDatabaseUseCase
{
    Task<ProcessResponse<AccessImportResultDto>> Execute(ProcessRequest<AccessImportModel> request);
}

public sealed class ImportAccessDatabaseUseCase : IImportAccessDatabaseUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IAccessImportMapper _accessImportMapper;
    private readonly IAccessImportRepository _accessImportRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ImportAccessDatabaseUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IAccessImportMapper accessImportMapper,
        IAccessImportRepository accessImportRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _accessImportMapper = accessImportMapper;
        _accessImportRepository = accessImportRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<AccessImportResultDto>> Execute(ProcessRequest<AccessImportModel> request)
    {
        var actorValidation = _clientUseCaseHelpers.ValidateActor(_actorTracker);
        if (!actorValidation.IsSuccess)
        {
            return ProcessResponse<AccessImportResultDto>.WithStatus(
                UseCaseStatus.Unauthorized,
                "Authentication required",
                ImportErrorCodes.ActorNotAuthenticated);
        }

        if (!OrganizationRoles.CanManageOrganizationUsers(_actorTracker.Role))
        {
            return ProcessResponse<AccessImportResultDto>.WithStatus(
                UseCaseStatus.Forbidden,
                "Administrator role is required",
                ImportErrorCodes.ActorNotAdmin);
        }

        var mapped = _accessImportMapper.Map(request.Payload, _nowProvider.UtcNow);
        if (mapped.Clients.Count == 0)
        {
            return ProcessResponse<AccessImportResultDto>.InvalidRequestResponse(
                "The Access file has no clients to import",
                ImportErrorCodes.NoClients);
        }

        var persist = await _accessImportRepository.ImportAsync(
            mapped,
            _actorTracker.TenantId!.Value,
            _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider),
            request.CancellationToken);

        if (persist.LockNotAcquired)
        {
            return ProcessResponse<AccessImportResultDto>.WithStatus(
                UseCaseStatus.Conflict,
                "Another import is already running for this organization",
                ImportErrorCodes.InProgress);
        }

        if (persist.TenantAlreadyHasClients)
        {
            return ProcessResponse<AccessImportResultDto>.WithStatus(
                UseCaseStatus.Conflict,
                "Import is only allowed when this organization has no clients",
                ImportErrorCodes.TenantNotEmpty);
        }

        return ProcessResponse<AccessImportResultDto>.Succeeded(new AccessImportResultDto
        {
            ClientsInserted = mapped.Clients.Count,
            MajorMedicalEnrollmentsInserted = mapped.MajorMedicalEnrollments.Count,
            DrugPlanEnrollmentsInserted = mapped.DrugPlanEnrollments.Count,
            SecondaryEnrollmentsInserted = mapped.SecondaryEnrollments.Count,
            InteractionsInserted = mapped.Interactions.Count,
            MedicarePlanNamesInserted = persist.MedicarePlanNamesInserted,
            DrugPlanNamesInserted = persist.DrugPlanNamesInserted,
            SecondaryPlanNamesInserted = persist.SecondaryPlanNamesInserted,
            Warnings = mapped.Warnings,
        });
    }
}
