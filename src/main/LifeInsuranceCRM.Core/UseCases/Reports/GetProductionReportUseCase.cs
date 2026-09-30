using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public interface IGetProductionReportUseCase
{
    Task<ProcessResponse<ProductionReportDto>> Execute(
        ProcessRequest<GetProductionReportRequest> request,
        bool recordView = true);
}

public sealed class GetProductionReportUseCase : IGetProductionReportUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IReportRepository _reportRepository;
    private readonly IReportUseCaseHelpers _reportUseCaseHelpers;
    private readonly ISecurityAudit _securityAudit;

    public GetProductionReportUseCase(
        IActorTracker actorTracker,
        IReportRepository reportRepository,
        IReportUseCaseHelpers reportUseCaseHelpers,
        ISecurityAudit securityAudit)
    {
        _actorTracker = actorTracker;
        _reportRepository = reportRepository;
        _reportUseCaseHelpers = reportUseCaseHelpers;
        _securityAudit = securityAudit;
    }

    public async Task<ProcessResponse<ProductionReportDto>> Execute(
        ProcessRequest<GetProductionReportRequest> request,
        bool recordView = true)
    {
        var validation = _reportUseCaseHelpers.ValidateViewer(_actorTracker);
        if (validation.IsFailed(out ProcessResponse<ProductionReportDto> failure))
        {
            return failure;
        }

        var yearValidation = _reportUseCaseHelpers.ValidatePlanYear(request.Payload.PlanYear);
        if (yearValidation.IsFailed(out ProcessResponse<ProductionReportDto> yearFailure))
        {
            if (recordView)
            {
                await _securityAudit.RecordAsync(
                    AuthSecurityEventTypes.ReportViewed,
                    success: false,
                    resource: $"production:{request.Payload.PlanYear}",
                    cancellationToken: request.CancellationToken,
                    httpStatus: SecurityAudit.StatusBadRequest,
                    detail: SecurityEventDetail.PlanYear(request.Payload.PlanYear),
                    failureReason: "Invalid plan year");
            }

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

        var report = new ProductionReportDto
        {
            PlanYear = request.Payload.PlanYear,
            Medicare = medicare,
            Drug = drug,
            Secondary = secondary,
        };
        if (recordView)
        {
            await _securityAudit.RecordAsync(
                AuthSecurityEventTypes.ReportViewed,
                success: true,
                resource: $"production:{request.Payload.PlanYear}",
                cancellationToken: request.CancellationToken,
                httpStatus: SecurityAudit.StatusOk,
                resultCount: report.Medicare.Count + report.Drug.Count + report.Secondary.Count,
                detail: SecurityEventDetail.PlanYear(request.Payload.PlanYear));
        }

        return ProcessResponse<ProductionReportDto>.Succeeded(report);
    }
}
