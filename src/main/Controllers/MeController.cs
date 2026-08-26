using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.OrganizationUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public MeController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> Get(
        [FromServices] IGetCurrentUserUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(true, cancellationToken)));
}
