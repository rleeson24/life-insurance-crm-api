using LifeInsuranceCRM.API.RateLimiting;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.UseCases.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/imports")]
[Authorize]
public sealed class ImportsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ImportsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpPost("access")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    [EnableRateLimiting(RateLimitingPolicyNames.SecuritySensitive)]
    [RequestSizeLimit(AccessImportLimits.MaxRequestBodyBytes)]
    public Task<IActionResult> ImportAccess(
        [FromBody] AccessImportModel model,
        [FromServices] IImportAccessDatabaseUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(
            useCase.Execute(_processRequestFactory.Create(model, cancellationToken)),
            created => new ObjectResult(created) { StatusCode = StatusCodes.Status201Created });
}
