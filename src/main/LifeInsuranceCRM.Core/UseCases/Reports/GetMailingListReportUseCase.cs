using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetMailingListReportUseCase
{
    Task<ProcessResponse<MailingListReportDto>> Execute(
        ProcessRequest<GetMailingListReportRequest> request,
        bool recordView = true);
}

public sealed class GetMailingListReportUseCase : IGetMailingListReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public GetMailingListReportUseCase(
        IActorTracker actorTracker,
        IReportRepository reportRepository,
        IReportUseCaseHelpers reportUseCaseHelpers,
        IAuthSecurityEventRecorder authSecurityEventRecorder)
    {
        _actorTracker = actorTracker;
        _reportRepository = reportRepository;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _authSecurityEventRecorder = authSecurityEventRecorder;
    }

    public async Task<ProcessResponse<MailingListReportDto>> Execute(
        ProcessRequest<GetMailingListReportRequest> request,
        bool recordView = true)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<MailingListReportDto> failure))
        {
            return failure;
        }

        var report = await _reportRepository.GetMailingListAsync(request.CancellationToken);
        if (recordView)
        {
            await SecurityAudit.RecordAsync(
                _authSecurityEventRecorder,
                AuthSecurityEventTypes.ReportViewed,
                success: true,
                resource: "mailing",
                cancellationToken: request.CancellationToken,
                httpStatus: SecurityAudit.StatusOk,
                resultCount: report.Items.Count);
        }

        return ProcessResponse<MailingListReportDto>.Succeeded(report);
    }
}
