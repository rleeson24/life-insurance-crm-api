using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
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
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public ExportBookOfBusinessReportUseCase(
        IActorTracker actorTracker,
        IGetBookOfBusinessReportUseCase getBookOfBusinessReportUseCase,
        IReportUseCaseHelpers reportUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _getBookOfBusinessReportUseCase = getBookOfBusinessReportUseCase;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<BookOfBusinessReportDto>> Execute(
        ProcessRequest<GetBookOfBusinessReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateExporter(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<BookOfBusinessReportDto> failure))
        {
            await _authSecurityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.ReportExported,
                success: false,
                failureReason: validation.Status == UseCaseStatus.Forbidden
                    ? "Export requires administrator role"
                    : "Authentication required",
                resource: Resource,
                cancellationToken: request.CancellationToken);
            return failure;
        }

        var report = await _getBookOfBusinessReportUseCase.Execute(request);
        if (!report.IsSuccess)
        {
            await _authSecurityEventRecorder.RecordAsync(
                AuthSecurityEventTypes.ReportExported,
                success: false,
                failureReason: "Report query failed",
                resource: Resource,
                cancellationToken: request.CancellationToken);
            return report;
        }

        await _authSecurityEventRecorder.RecordAsync(
            AuthSecurityEventTypes.ReportExported,
            success: true,
            resource: Resource,
            cancellationToken: request.CancellationToken);
        return report;
    }
}
