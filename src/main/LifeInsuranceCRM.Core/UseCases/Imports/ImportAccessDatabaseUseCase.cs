using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Import;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Http;

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
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImportAccessDatabaseUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IAccessImportMapper accessImportMapper,
        IAccessImportRepository accessImportRepository,
        IClientUseCaseHelpers clientUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder,
        IHttpContextAccessor httpContextAccessor)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _accessImportMapper = accessImportMapper;
        _accessImportRepository = accessImportRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
        _httpContextAccessor = httpContextAccessor;
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
            await RecordImportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: SecurityAudit.StatusBadRequest,
                outcome: "empty",
                mapped: mapped,
                result: null,
                failureReason: "No clients to import");
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
            await RecordImportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: SecurityAudit.StatusConflict,
                outcome: "inProgress",
                mapped: mapped,
                result: null,
                failureReason: "Import already running");
            return ProcessResponse<AccessImportResultDto>.WithStatus(
                UseCaseStatus.Conflict,
                "Another import is already running for this organization",
                ImportErrorCodes.InProgress);
        }

        if (persist.TenantAlreadyHasClients)
        {
            await RecordImportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: SecurityAudit.StatusConflict,
                outcome: "rejected",
                mapped: mapped,
                result: null,
                failureReason: "Organization already has clients");
            return ProcessResponse<AccessImportResultDto>.WithStatus(
                UseCaseStatus.Conflict,
                "Import is only allowed when this organization has no clients",
                ImportErrorCodes.TenantNotEmpty);
        }

        var imported = new AccessImportResultDto
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
        };
        await RecordImportAsync(
            request.CancellationToken,
            success: true,
            httpStatus: SecurityAudit.StatusCreated,
            outcome: "inserted",
            mapped: mapped,
            result: imported,
            failureReason: null);
        return ProcessResponse<AccessImportResultDto>.Succeeded(imported);
    }

    private Task RecordImportAsync(
        CancellationToken cancellationToken,
        bool success,
        int httpStatus,
        string outcome,
        MappedAccessImport mapped,
        AccessImportResultDto? result,
        string? failureReason)
    {
        var clients = result?.ClientsInserted ?? mapped.Clients.Count;
        var majorMedical = result?.MajorMedicalEnrollmentsInserted ?? mapped.MajorMedicalEnrollments.Count;
        var drug = result?.DrugPlanEnrollmentsInserted ?? mapped.DrugPlanEnrollments.Count;
        var secondary = result?.SecondaryEnrollmentsInserted ?? mapped.SecondaryEnrollments.Count;
        var interactions = result?.InteractionsInserted ?? mapped.Interactions.Count;
        var warnings = result?.Warnings.Count ?? mapped.Warnings.Count;
        return SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.DataImported,
            success,
            resource: "access",
            cancellationToken,
            httpStatus,
            resultCount: success ? clients : 0,
            detail: SecurityEventDetail.Import(
                _httpContextAccessor.HttpContext?.Request.ContentLength,
                clients,
                majorMedical,
                drug,
                secondary,
                interactions,
                warnings,
                outcome),
            failureReason: failureReason);
    }
}
