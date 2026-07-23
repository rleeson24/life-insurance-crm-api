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
[Route("api/clients/{clientId:guid}/medicare-enrollments")]
[Authorize]
public sealed class ClientMedicareEnrollmentsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ClientMedicareEnrollmentsController(
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
        [FromServices] IListMedicareEnrollmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListMedicareEnrollmentsRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Create(
        Guid clientId,
        [FromBody] CreateMedicareEnrollmentModel model,
        [FromServices] ICreateMedicareEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpPut("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Update(
        Guid clientId,
        Guid enrollmentId,
        [FromBody] UpdateMedicareEnrollmentModel model,
        [FromServices] IUpdateMedicareEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(
            model with { ClientId = clientId, MedicareEnrollmentId = enrollmentId },
            cancellationToken)));

    [HttpDelete("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Delete(
        Guid clientId,
        Guid enrollmentId,
        [FromServices] IDeleteMedicareEnrollmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeleteMedicareEnrollmentRequest
        {
            ClientId = clientId,
            MedicareEnrollmentId = enrollmentId,
        };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
