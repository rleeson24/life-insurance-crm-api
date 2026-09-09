using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetBookOfBusinessReportUseCase
{
    Task<ProcessResponse<BookOfBusinessReportDto>> Execute(ProcessRequest<GetBookOfBusinessReportRequest> request);
}

public sealed class GetBookOfBusinessReportUseCase : IGetBookOfBusinessReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;

    public GetBookOfBusinessReportUseCase(
        IActorTracker actorTracker,
        IReportRepository reportRepository,
        IReportUseCaseHelpers reportUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _reportRepository = reportRepository;
        _reportUseCaseHelpers = reportUseCaseHelpers;
    }

    public async Task<ProcessResponse<BookOfBusinessReportDto>> Execute(
        ProcessRequest<GetBookOfBusinessReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<BookOfBusinessReportDto> failure))
        {
            return failure;
        }

        var report = await _reportRepository.GetBookOfBusinessAsync(request.CancellationToken);
        return ProcessResponse<BookOfBusinessReportDto>.Succeeded(report);
    }
}
