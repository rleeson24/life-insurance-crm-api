using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IExportRetentionReportUseCase
{
    Task<ProcessResponse<RetentionReportDto>> Execute(ProcessRequest<GetRetentionReportRequest> request);
}

public sealed class ExportRetentionReportUseCase : IExportRetentionReportUseCase
{
    private const string Resource = "retention";

    private readonly IActorTracker _actorTracker;
    private readonly IGetRetentionReportUseCase _getRetentionReportUseCase;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public ExportRetentionReportUseCase(
        IActorTracker actorTracker,
        IGetRetentionReportUseCase getRetentionReportUseCase,
        IReportUseCaseHelpers reportUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _getRetentionReportUseCase = getRetentionReportUseCase;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<RetentionReportDto>> Execute(
        ProcessRequest<GetRetentionReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateExporter(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<RetentionReportDto> failure))
        {
            await RecordExportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: SecurityAudit.FromStatus(validation.Status),
                failureReason: validation.Status == UseCaseStatus.Forbidden
                    ? "Export requires administrator role"
                    : "Authentication required");
            return failure;
        }

        var report = await _getRetentionReportUseCase.Execute(request, recordView: false);
        if (!report.IsSuccess)
        {
            await RecordExportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: SecurityAudit.FromStatus(report.Status),
                failureReason: "Report query failed");
            return report;
        }

        await RecordExportAsync(
            request.CancellationToken,
            success: true,
            httpStatus: SecurityAudit.StatusOk,
            resultCount: report.Result!.Rows.Count);
        return report;
    }

    private Task RecordExportAsync(
        CancellationToken cancellationToken,
        bool success,
        int httpStatus,
        int? resultCount = null,
        string? failureReason = null) =>
        SecurityAudit.RecordAsync(
            _authSecurityEventRecorder,
            AuthSecurityEventTypes.ReportExported,
            success,
            Resource,
            cancellationToken,
            httpStatus,
            resultCount,
            failureReason: failureReason);
}
