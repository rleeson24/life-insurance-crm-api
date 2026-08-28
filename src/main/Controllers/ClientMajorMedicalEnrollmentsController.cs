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
[Route("api/clients/{clientId:guid}/major-medical-enrollments")]
[Authorize]
public sealed class ClientMajorMedicalEnrollmentsController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public ClientMajorMedicalEnrollmentsController(
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
        [FromServices] IListMajorMedicalEnrollmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new ListMajorMedicalEnrollmentsRequest { ClientId = clientId };
        return FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Create(
        Guid clientId,
        [FromBody] CreateMajorMedicalEnrollmentModel model,
        [FromServices] ICreateMajorMedicalEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(model with { ClientId = clientId }, cancellationToken)));

    [HttpPut("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanWrite)]
    public Task<IActionResult> Update(
        Guid clientId,
        Guid enrollmentId,
        [FromBody] UpdateMajorMedicalEnrollmentModel model,
        [FromServices] IUpdateMajorMedicalEnrollmentUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(
            model with { ClientId = clientId, MajorMedicalEnrollmentId = enrollmentId },
            cancellationToken)));

    [HttpDelete("{enrollmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Delete(
        Guid clientId,
        Guid enrollmentId,
        [FromServices] IDeleteMajorMedicalEnrollmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var request = new DeleteMajorMedicalEnrollmentRequest
        {
            ClientId = clientId,
            MajorMedicalEnrollmentId = enrollmentId,
        };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
