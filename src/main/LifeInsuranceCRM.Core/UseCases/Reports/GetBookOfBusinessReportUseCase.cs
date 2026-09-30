using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetBookOfBusinessReportUseCase
{
    Task<ProcessResponse<BookOfBusinessReportDto>> Execute(
        ProcessRequest<GetBookOfBusinessReportRequest> request,
        bool recordView = true);
}

public sealed class GetBookOfBusinessReportUseCase : IGetBookOfBusinessReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public GetBookOfBusinessReportUseCase(
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

    public async Task<ProcessResponse<BookOfBusinessReportDto>> Execute(
        ProcessRequest<GetBookOfBusinessReportRequest> request,
        bool recordView = true)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<BookOfBusinessReportDto> failure))
        {
            return failure;
        }

        var report = await _reportRepository.GetBookOfBusinessAsync(request.CancellationToken);
        if (recordView)
        {
            await SecurityAudit.RecordAsync(
                _authSecurityEventRecorder,
                AuthSecurityEventTypes.ReportViewed,
                success: true,
                resource: "book",
                cancellationToken: request.CancellationToken,
                httpStatus: SecurityAudit.StatusOk,
                resultCount: report.Items.Count);
        }

        return ProcessResponse<BookOfBusinessReportDto>.Succeeded(report);
    }
}
