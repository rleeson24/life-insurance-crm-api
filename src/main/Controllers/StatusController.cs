using LifeInsuranceCRM.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

[ApiController]
[Route("api/status")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        Ok(new { status = "ok", service = "LifeInsuranceCRM.API" });

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult GetAuthenticated() =>
        Ok(new { status = "ok", authenticated = true });
}
