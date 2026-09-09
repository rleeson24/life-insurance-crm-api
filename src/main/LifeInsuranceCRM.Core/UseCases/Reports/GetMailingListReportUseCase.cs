using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetMailingListReportUseCase
{
    Task<ProcessResponse<MailingListReportDto>> Execute(ProcessRequest<GetMailingListReportRequest> request);
}

public sealed class GetMailingListReportUseCase : IGetMailingListReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;

    public GetMailingListReportUseCase(
        IActorTracker actorTracker,
        IReportRepository reportRepository,
        IReportUseCaseHelpers reportUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _reportRepository = reportRepository;
        _reportUseCaseHelpers = reportUseCaseHelpers;
    }

    public async Task<ProcessResponse<MailingListReportDto>> Execute(
        ProcessRequest<GetMailingListReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<MailingListReportDto> failure))
        {
            return failure;
        }

        var report = await _reportRepository.GetMailingListAsync(request.CancellationToken);
        return ProcessResponse<MailingListReportDto>.Succeeded(report);
    }
}
