using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IExportMailingListReportUseCase
{
    Task<ProcessResponse<MailingListReportDto>> Execute(ProcessRequest<GetMailingListReportRequest> request);
}

public sealed class ExportMailingListReportUseCase : IExportMailingListReportUseCase
{
    private const string Resource = "mailing";

    private readonly IActorTracker _actorTracker;
    private readonly IGetMailingListReportUseCase _getMailingListReportUseCase;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public ExportMailingListReportUseCase(
        IActorTracker actorTracker,
        IGetMailingListReportUseCase getMailingListReportUseCase,
        IReportUseCaseHelpers reportUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _getMailingListReportUseCase = getMailingListReportUseCase;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<MailingListReportDto>> Execute(
        ProcessRequest<GetMailingListReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateExporter(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<MailingListReportDto> failure))
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

        var report = await _getMailingListReportUseCase.Execute(request);
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
