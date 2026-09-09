using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
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

        var report = await _getRetentionReportUseCase.Execute(request);
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
