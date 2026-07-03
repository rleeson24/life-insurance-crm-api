using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public sealed class ClientsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ClientsController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] ListClientsRequest request,
        [FromServices] IListClientsUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));

    [HttpGet("{clientId:guid}")]
    public Task<IActionResult> Get(
        Guid clientId,
        [FromServices] IGetClientUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new GetClientRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpGet("{clientId:guid}/detail")]
    public Task<IActionResult> GetDetail(
        Guid clientId,
        [FromServices] IGetClientDetailUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new GetClientDetailRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateClientModel model,
        [FromServices] ICreateClientUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(
            useCase.Execute(_processRequestFactory.Create(model, cancellationToken)),
            client => CreatedAtAction(nameof(Get), new { clientId = client.ClientId }, client));

    [HttpPut("{clientId:guid}")]
    public Task<IActionResult> Update(
        Guid clientId,
        [FromBody] UpdateClientModel model,
        [FromServices] IUpdateClientUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpPatch("{clientId:guid}")]
    public Task<IActionResult> UpdateStatus(
        Guid clientId,
        [FromBody] UpdateClientStatusModel model,
        [FromServices] IUpdateClientStatusUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpDelete("{clientId:guid}")]
    public Task<IActionResult> Delete(
        Guid clientId,
        [FromServices] IDeleteClientUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeleteClientRequest { ClientId = clientId };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
