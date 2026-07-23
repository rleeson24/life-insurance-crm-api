using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/follow-ups")]
[Authorize]
public sealed class FollowUpsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public FollowUpsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanRead)]
    public Task<IActionResult> List(
        [FromServices] IListFollowUpInteractionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListFollowUpInteractionsRequest();
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }
}
