using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IReportUseCaseHelpers
{
    ProcessResponse<bool> ValidateViewer(IActorTracker actorTracker);

    ProcessResponse<bool> ValidateExporter(IActorTracker actorTracker);

    ProcessResponse<bool> ValidatePlanYear(short planYear);
}

public sealed class ReportUseCaseHelpers : IReportUseCaseHelpers
{
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ReportUseCaseHelpers(IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public ProcessResponse<bool> ValidateViewer(IActorTracker actorTracker)
    {
        var validation = _clientUseCaseHelpers.ValidateActor(actorTracker);
        if (!validation.IsSuccess)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Unauthorized,
                "Authentication required",
                ReportErrorCodes.ActorNotAuthenticated);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    public ProcessResponse<bool> ValidateExporter(IActorTracker actorTracker)
    {
        var validation = ValidateViewer(actorTracker);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (!string.Equals(actorTracker.Role, OrganizationRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Forbidden,
                "Administrator role is required",
                ReportErrorCodes.ActorNotAdmin);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    public ProcessResponse<bool> ValidatePlanYear(short planYear)
    {
        if (planYear < ReportConstants.MinPlanYear || planYear > ReportConstants.MaxPlanYear)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Plan year must be between {ReportConstants.MinPlanYear} and {ReportConstants.MaxPlanYear}",
                ReportErrorCodes.PlanYearInvalid);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
