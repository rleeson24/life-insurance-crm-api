using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/auth-security-events")]
[Authorize(Policy = AuthorizationPolicies.CanManagePlatform)]
public sealed class AuthSecurityEventsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public AuthSecurityEventsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] ListAuthSecurityEventsRequest request,
        [FromServices] IListAuthSecurityEventsUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));

    [HttpGet("{authSecurityEventId:guid}")]
    public Task<IActionResult> Get(
        Guid authSecurityEventId,
        [FromServices] IGetAuthSecurityEventUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new GetAuthSecurityEventRequest { AuthSecurityEventId = authSecurityEventId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }
}
