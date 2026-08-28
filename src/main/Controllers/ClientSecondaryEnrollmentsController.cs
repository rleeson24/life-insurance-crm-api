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
[Route("api/clients/{clientId:guid}/secondary-enrollments")]
[Authorize]
public sealed class ClientSecondaryEnrollmentsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ClientSecondaryEnrollmentsController(
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
        [FromServices] IListSecondaryEnrollmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListSecondaryEnrollmentsRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Create(
        Guid clientId,
        [FromBody] CreateSecondaryEnrollmentModel model,
        [FromServices] ICreateSecondaryEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpPut("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Update(
        Guid clientId,
        Guid enrollmentId,
        [FromBody] UpdateSecondaryEnrollmentModel model,
        [FromServices] IUpdateSecondaryEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(
            model with { ClientId = clientId, SecondaryEnrollmentId = enrollmentId },
            cancellationToken)));

    [HttpDelete("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Delete(
        Guid clientId,
        Guid enrollmentId,
        [FromServices] IDeleteSecondaryEnrollmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeleteSecondaryEnrollmentRequest
        {
            ClientId = clientId,
            SecondaryEnrollmentId = enrollmentId,
        };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
