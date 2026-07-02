using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    private readonly IProcessResponseActionMapper _actionMapper;

    protected ApiControllerBase(IProcessResponseActionMapper actionMapper)
    {
        _actionMapper = actionMapper;
    }

    protected async Task<IActionResult> FromUseCase<T>(
        Task<ProcessResponse<T>> useCaseTask,
        Func<T, IActionResult>? onSuccess = null)
    {
        var response = await useCaseTask;
        return _actionMapper.Map(response, HttpContext, onSuccess);
    }
}
