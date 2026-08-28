using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/clients/{clientId:guid}/drug-plan-enrollments")]
[Authorize]
public sealed class ClientDrugPlanEnrollmentsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ClientDrugPlanEnrollmentsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> List(
        Guid clientId,
        [FromServices] IListDrugPlanEnrollmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListDrugPlanEnrollmentsRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Create(
        Guid clientId,
        [FromBody] CreateDrugPlanEnrollmentModel model,
        [FromServices] ICreateDrugPlanEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpPut("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Update(
        Guid clientId,
        Guid enrollmentId,
        [FromBody] UpdateDrugPlanEnrollmentModel model,
        [FromServices] IUpdateDrugPlanEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(
            model with { ClientId = clientId, DrugPlanEnrollmentId = enrollmentId },
            cancellationToken)));

    [HttpDelete("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Delete(
        Guid clientId,
        Guid enrollmentId,
        [FromServices] IDeleteDrugPlanEnrollmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeleteDrugPlanEnrollmentRequest
        {
            ClientId = clientId,
            DrugPlanEnrollmentId = enrollmentId,
        };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
