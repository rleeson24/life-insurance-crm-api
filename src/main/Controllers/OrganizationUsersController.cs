using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.OrganizationUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/organization-users")]
[Authorize]
public sealed class OrganizationUsersController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public OrganizationUsersController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> List(
        [FromQuery] ListOrganizationUsersRequest request,
        [FromServices] IListOrganizationUsersUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(_processRequestFactory.Create(request, cancellationToken)));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Create(
        [FromBody] CreateOrganizationUserModel model,
        [FromServices] ICreateOrganizationUserUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(
            useCase.Execute(_processRequestFactory.Create(model, cancellationToken)),
            user => new ObjectResult(user) { StatusCode = StatusCodes.Status201Created });

    [HttpPatch("{organizationUserId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDelete)]
    public Task<IActionResult> Update(
        Guid organizationUserId,
        [FromBody] UpdateOrganizationUserModel model,
        [FromServices] IUpdateOrganizationUserUseCase useCase,
        CancellationToken cancellationToken) =>
        FromUseCase(useCase.Execute(
            _processRequestFactory.Create(
                model with { OrganizationUserId = organizationUserId },
                cancellationToken)));
}
