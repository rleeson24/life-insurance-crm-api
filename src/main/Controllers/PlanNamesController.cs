using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.PlanNames;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/plan-names/{kind}")]
[Authorize]
public sealed class PlanNamesController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public PlanNamesController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> List(
        PlanNameKind kind,
        [FromQuery] short year,
        [FromServices] IListPlanNamesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListPlanNamesRequest { Kind = kind, PlanYear = year };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpGet("lookup")]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> Lookup(
        PlanNameKind kind,
        [FromQuery] short fromYear,
        [FromQuery] short toYear,
        [FromServices] ILookupPlanNamesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new LookupPlanNamesRequest
        {
            Kind = kind,
            FromYear = fromYear,
            ToYear = toYear,
        };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Create(
        PlanNameKind kind,
        [FromBody] CreatePlanNameModel model,
        [FromServices] ICreatePlanNameUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(
            useCase.Execute(_processRequestFactory.Create(model with { Kind = kind }, cancellationToken)),
            created => new ObjectResult(created) { StatusCode = StatusCodes.Status201Created });

    [HttpPut("{planNameId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Update(
        PlanNameKind kind,
        Guid planNameId,
        [FromBody] UpdatePlanNameModel model,
        [FromServices] IUpdatePlanNameUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(
            model with { Kind = kind, PlanNameId = planNameId },
            cancellationToken)));

    [HttpDelete("{planNameId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Delete(
        PlanNameKind kind,
        Guid planNameId,
        [FromServices] IDeletePlanNameUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeletePlanNameRequest { Kind = kind, PlanNameId = planNameId };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }

    [HttpPost("clone")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Clone(
        PlanNameKind kind,
        [FromBody] ClonePlanNamesModel model,
        [FromServices] IClonePlanNamesUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(model with { Kind = kind }, cancellationToken)));
}
