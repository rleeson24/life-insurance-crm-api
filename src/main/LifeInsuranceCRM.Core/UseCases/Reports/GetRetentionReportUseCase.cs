using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetRetentionReportUseCase
{
    Task<ProcessResponse<RetentionReportDto>> Execute(
        ProcessRequest<GetRetentionReportRequest> request,
        bool recordView = true);
}

public sealed class GetRetentionReportUseCase : IGetRetentionReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly IAuthSecurityEventRecorder _authSecurityEventRecorder;

    public GetRetentionReportUseCase(
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

    public async Task<ProcessResponse<RetentionReportDto>> Execute(
        ProcessRequest<GetRetentionReportRequest> request,
        bool recordView = true)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<RetentionReportDto> failure))
        {
            return failure;
        }

        var rows = await _reportRepository.ListRetentionAsync(request.CancellationToken);
        if (recordView)
        {
            await SecurityAudit.RecordAsync(
                _authSecurityEventRecorder,
                AuthSecurityEventTypes.ReportViewed,
                success: true,
                resource: "retention",
                cancellationToken: request.CancellationToken,
                httpStatus: SecurityAudit.StatusOk,
                resultCount: rows.Count);
        }

        return ProcessResponse<RetentionReportDto>.Succeeded(new RetentionReportDto { Rows = rows });
    }
}
