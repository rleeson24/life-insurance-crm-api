using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
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
    private readonly ISecurityAudit _securityAudit;

    public ExportProductionReportUseCase(
        IActorTracker actorTracker,
        IGetProductionReportUseCase getProductionReportUseCase,
        IReportUseCaseHelpers reportUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _getProductionReportUseCase = getProductionReportUseCase;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _securityAudit = securityAudit;
    }

    public async Task<ProcessResponse<ProductionReportDto>> Execute(
        ProcessRequest<GetProductionReportRequest> request)
    {
        var resource = $"production:{request.Payload.PlanYear}";
        var detail = SecurityEventDetail.PlanYear(request.Payload.PlanYear);
        var validation = _reportUseCaseHelpers.ValidateExporter(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<ProductionReportDto> failure))
        {
            await RecordExportAsync(
                request.CancellationToken,
                resource,
                detail,
                success: false,
                httpStatus: _securityAudit.FromStatus(validation.Status),
                failureReason: validation.Status == UseCaseStatus.Forbidden
                    ? "Export requires administrator role"
                    : "Authentication required");
            return failure;
        }

        var report = await _getProductionReportUseCase.Execute(request, recordView: false);
        if (!report.IsSuccess)
        {
            await RecordExportAsync(
                request.CancellationToken,
                resource,
                detail,
                success: false,
                httpStatus: _securityAudit.FromStatus(report.Status),
                failureReason: report.Status == UseCaseStatus.InvalidRequest
                    ? "Invalid plan year"
                    : "Report query failed");
            return report;
        }

        var result = report.Result!;
        await RecordExportAsync(
            request.CancellationToken,
            resource,
            detail,
            success: true,
            httpStatus: SecurityAudit.StatusOk,
            resultCount: result.Medicare.Count + result.Drug.Count + result.Secondary.Count);
        return report;
    }

    private Task RecordExportAsync(
        CancellationToken cancellationToken,
        string resource,
        string detail,
        bool success,
        int httpStatus,
        int? resultCount = null,
        string? failureReason = null) =>
        _securityAudit.RecordAsync(
            AuthSecurityEventTypes.ReportExported,
            success,
            resource,
            cancellationToken,
            httpStatus,
            resultCount,
            detail: detail,
            failureReason: failureReason);
}
