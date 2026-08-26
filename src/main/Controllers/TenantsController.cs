using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.UseCases.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = AuthorizationPolicies.CanManagePlatform)]
public sealed class TenantsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public TenantsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    public Task<IActionResult> List(
        [FromServices] IListTenantsUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(true, cancellationToken)));

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateTenantModel model,
        [FromServices] ICreateTenantUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(
            useCase.Execute(_processRequestFactory.Create(model, cancellationToken)),
            tenant => new ObjectResult(tenant) { StatusCode = StatusCodes.Status201Created });

    [HttpPatch("{tenantId:guid}")]
    public Task<IActionResult> Update(
        Guid tenantId,
        [FromBody] UpdateTenantModel model,
        [FromServices] IUpdateTenantUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(
                model with { TenantId = tenantId },
                cancellationToken)));
}
