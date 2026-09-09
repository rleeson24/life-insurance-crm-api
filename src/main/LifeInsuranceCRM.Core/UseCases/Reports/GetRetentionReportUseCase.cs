using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetRetentionReportUseCase
{
    Task<ProcessResponse<RetentionReportDto>> Execute(ProcessRequest<GetRetentionReportRequest> request);
}

public sealed class GetRetentionReportUseCase : IGetRetentionReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;

    public GetRetentionReportUseCase(
        IActorTracker actorTracker,
        IReportRepository reportRepository,
        IReportUseCaseHelpers reportUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _reportRepository = reportRepository;
        _reportUseCaseHelpers = reportUseCaseHelpers;
    }

    public async Task<ProcessResponse<RetentionReportDto>> Execute(
        ProcessRequest<GetRetentionReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<RetentionReportDto> failure))
        {
            return failure;
        }

        var rows = await _reportRepository.ListRetentionAsync(request.CancellationToken);
        return ProcessResponse<RetentionReportDto>.Succeeded(new RetentionReportDto { Rows = rows });
    }
}
