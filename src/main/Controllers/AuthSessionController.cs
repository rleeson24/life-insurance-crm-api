using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthSessionController : ApiControllerBase
{
    private readonly IProcessRequestFactory _processRequestFactory;

    public AuthSessionController(
        IProcessResponseActionMapper actionMapper,
        IProcessRequestFactory processRequestFactory)
        : base(actionMapper)
    {
        _processRequestFactory = processRequestFactory;
    }

    [HttpPost("login")]
    [Authorize]
    public Task<IActionResult> Login(
        [FromServices] IRecordAuthSessionEventUseCase useCase,
        CancellationToken cancellationToken) =>
        Record(useCase, AuthSecurityEventTypes.LoginSucceeded, failureReason: null, cancellationToken);

    [HttpPost("logout")]
    [Authorize]
    public Task<IActionResult> Logout(
        [FromServices] IRecordAuthSessionEventUseCase useCase,
        CancellationToken cancellationToken) =>
        Record(useCase, AuthSecurityEventTypes.Logout, failureReason: null, cancellationToken);

    [HttpPost("login-failed")]
    [AllowAnonymous]
    public Task<IActionResult> LoginFailed(
        [FromBody] RecordLoginFailureModel? model,
        [FromServices] IRecordAuthSessionEventUseCase useCase,
        CancellationToken cancellationToken) =>
        Record(
            useCase,
            AuthSecurityEventTypes.LoginFailed,
            model?.Reason,
            cancellationToken);

    private Task<IActionResult> Record(
        IRecordAuthSessionEventUseCase useCase,
        string eventType,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var request = new RecordAuthSessionEventRequest
        {
            EventType = eventType,
            FailureReason = failureReason,
        };
        return FromUseCase(
            useCase.Execute(_processRequestFactory.Create(request, cancellationToken)),
            _ => NoContent());
    }
}
