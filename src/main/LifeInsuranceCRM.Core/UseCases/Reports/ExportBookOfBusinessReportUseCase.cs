using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IExportBookOfBusinessReportUseCase
{
    Task<ProcessResponse<BookOfBusinessReportDto>> Execute(ProcessRequest<GetBookOfBusinessReportRequest> request);
}

public sealed class ExportBookOfBusinessReportUseCase : IExportBookOfBusinessReportUseCase
{
    private const string Resource = "book";

    private readonly IActorTracker _actorTracker;
    private readonly IGetBookOfBusinessReportUseCase _getBookOfBusinessReportUseCase;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly ISecurityAudit _securityAudit;

    public ExportBookOfBusinessReportUseCase(
        IActorTracker actorTracker,
        IGetBookOfBusinessReportUseCase getBookOfBusinessReportUseCase,
        IReportUseCaseHelpers reportUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _getBookOfBusinessReportUseCase = getBookOfBusinessReportUseCase;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _securityAudit = securityAudit;
    }

    public async Task<ProcessResponse<BookOfBusinessReportDto>> Execute(
        ProcessRequest<GetBookOfBusinessReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateExporter(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<BookOfBusinessReportDto> failure))
        {
            await RecordExportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: _securityAudit.FromStatus(validation.Status),
                failureReason: validation.Status == UseCaseStatus.Forbidden
                    ? "Export requires administrator role"
                    : "Authentication required");
            return failure;
        }

        var report = await _getBookOfBusinessReportUseCase.Execute(request, recordView: false);
        if (!report.IsSuccess)
        {
            await RecordExportAsync(
                request.CancellationToken,
                success: false,
                httpStatus: _securityAudit.FromStatus(report.Status),
                failureReason: "Report query failed");
            return report;
        }

        await RecordExportAsync(
            request.CancellationToken,
            success: true,
            httpStatus: SecurityAudit.StatusOk,
            resultCount: report.Result!.Items.Count);
        return report;
    }

    private Task RecordExportAsync(
        CancellationToken cancellationToken,
        bool success,
        int httpStatus,
        int? resultCount = null,
        string? failureReason = null) =>
        _securityAudit.RecordAsync(
            AuthSecurityEventTypes.ReportExported,
            success,
            Resource,
            cancellationToken,
            httpStatus,
            resultCount,
            failureReason: failureReason);
}
