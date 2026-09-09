using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetProductionReportUseCase
{
    Task<ProcessResponse<ProductionReportDto>> Execute(ProcessRequest<GetProductionReportRequest> request);
}

public sealed class GetProductionReportUseCase : IGetProductionReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;

    public GetProductionReportUseCase(
        IActorTracker actorTracker,
        IReportRepository reportRepository,
        IReportUseCaseHelpers reportUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _reportRepository = reportRepository;
        _reportUseCaseHelpers = reportUseCaseHelpers;
    }

    public async Task<ProcessResponse<ProductionReportDto>> Execute(
        ProcessRequest<GetProductionReportRequest> request)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<ProductionReportDto> failure))
        {
            return failure;
        }

        var yearValidation = _reportUseCaseHelpers.ValidatePlanYear(request.Payload.PlanYear);
        if (yearValidation.IsFailed(out ProcessResponse<ProductionReportDto> yearFailure))
        {
            return yearFailure;
        }

        var medicare = await _reportRepository.ListMedicareProductionAsync(
            request.Payload.PlanYear,
            request.CancellationToken);
        var drug = await _reportRepository.ListDrugProductionAsync(
            request.Payload.PlanYear,
            request.CancellationToken);
        var secondary = await _reportRepository.ListSecondaryProductionAsync(
            request.Payload.PlanYear,
            request.CancellationToken);

        return ProcessResponse<ProductionReportDto>.Succeeded(new ProductionReportDto
        {
            PlanYear = request.Payload.PlanYear,
            Medicare = medicare,
            Drug = drug,
            Secondary = secondary,
        });
    }
}
