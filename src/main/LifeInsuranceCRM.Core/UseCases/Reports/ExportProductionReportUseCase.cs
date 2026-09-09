using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IExportProductionReportUseCase
{
    Task<ProcessResponse<ProductionReportDto>> Execute(ProcessRequest<GetProductionReportRequest> request);
}

public sealed class ExportProductionReportUseCase : IExportProductionReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IGetProductionReportUseCase _getProductionReportUseCase;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public ExportProductionReportUseCase(
        IActorTracker actorTracker,
        IGetProductionReportUseCase getProductionReportUseCase,
        IReportUseCaseHelpers reportUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _getProductionReportUseCase = getProductionReportUseCase;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<ProductionReportDto>> Execute(
        ProcessRequest<GetProductionReportRequest> request)
    {
        var resource = $"production:{request.Payload.PlanYear}";
        var validation = _reportUseCaseHelpers.ValidateExporter(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<ProductionReportDto> failure))
        {
            await _authSecurityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.ReportExported,
                success: false,
                failureReason: validation.Status == UseCaseStatus.Forbidden
                    ? "Export requires administrator role"
                    : "Authentication required",
                resource: resource,
                cancellationToken: request.CancellationToken);
            return failure;
        }

        var report = await _getProductionReportUseCase.Execute(request);
        if (!report.IsSuccess)
        {
            await _authSecurityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.ReportExported,
                success: false,
                failureReason: report.Status == UseCaseStatus.InvalidRequest
                    ? "Invalid plan year"
                    : "Report query failed",
                resource: resource,
                cancellationToken: request.CancellationToken);
            return report;
        }

        await _authSecurityEventRecorder.RecordAsync(
            AuthSecurityEventTypes.ReportExported,
            success: true,
            resource: resource,
            cancellationToken: request.CancellationToken);
        return report;
    }
}
