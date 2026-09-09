using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ReportsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet("book")]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> GetBook(
        [FromServices] IGetBookOfBusinessReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetBookOfBusinessReportRequest(), cancellationToken)));

    [HttpGet("mailing")]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> GetMailingList(
        [FromServices] IGetMailingListReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetMailingListReportRequest(), cancellationToken)));

    [HttpGet("production")]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> GetProduction(
        [FromQuery] short planYear,
        [FromServices] IGetProductionReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetProductionReportRequest { PlanYear = planYear }, cancellationToken)));

    [HttpGet("retention")]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> GetRetention(
        [FromServices] IGetRetentionReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetRetentionReportRequest(), cancellationToken)));

    [HttpPost("book/export")]
    [Authorize(Policy = AuthorizationPolicies.CanExportReports)]
    public Task<IActionResult> ExportBook(
        [FromServices] IExportBookOfBusinessReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetBookOfBusinessReportRequest(), cancellationToken)));

    [HttpPost("mailing/export")]
    [Authorize(Policy = AuthorizationPolicies.CanExportReports)]
    public Task<IActionResult> ExportMailingList(
        [FromServices] IExportMailingListReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetMailingListReportRequest(), cancellationToken)));

    [HttpPost("production/export")]
    [Authorize(Policy = AuthorizationPolicies.CanExportReports)]
    public Task<IActionResult> ExportProduction(
        [FromQuery] short planYear,
        [FromServices] IExportProductionReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetProductionReportRequest { PlanYear = planYear }, cancellationToken)));

    [HttpPost("retention/export")]
    [Authorize(Policy = AuthorizationPolicies.CanExportReports)]
    public Task<IActionResult> ExportRetention(
        [FromServices] IExportRetentionReportUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(new GetRetentionReportRequest(), cancellationToken)));
}
