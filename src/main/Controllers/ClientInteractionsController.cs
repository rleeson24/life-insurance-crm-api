using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/clients/{clientId:guid}/interactions")]
[Authorize]
public sealed class ClientInteractionsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ClientInteractionsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    public Task<IActionResult> List(
        Guid clientId,
        [FromServices] IListClientInteractionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListClientInteractionsRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    public Task<IActionResult> Create(
        Guid clientId,
        [FromBody] CreateClientInteractionModel model,
        [FromServices] ICreateClientInteractionUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpPut("{interactionId:guid}")]
    public Task<IActionResult> Update(
        Guid clientId,
        Guid interactionId,
        [FromBody] UpdateClientInteractionModel model,
        [FromServices] IUpdateClientInteractionUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(
            model with { ClientId = clientId, ClientInteractionId = interactionId },
            cancellationToken)));

    [HttpDelete("{interactionId:guid}")]
    public Task<IActionResult> Delete(
        Guid clientId,
        Guid interactionId,
        [FromServices] IDeleteClientInteractionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeleteClientInteractionRequest
        {
            ClientId = clientId,
            ClientInteractionId = interactionId,
        };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
