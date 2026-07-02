using LifeInsuranceCRM.API.Models;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Services;

public interface IProcessResponseActionMapper
{
    IActionResult Map<T>(
        ProcessResponse<T> response,
        HttpContext httpContext,
        Func<T, IActionResult>? onSuccess = null);
}

public sealed class ProcessResponseActionMapper : IProcessResponseActionMapper
{
    private readonly IProblemDetailsFactory _problemDetailsFactory;

    public ProcessResponseActionMapper(IProblemDetailsFactory problemDetailsFactory)
    {
        _problemDetailsFactory = problemDetailsFactory;
    }

    public IActionResult Map<T>(
        ProcessResponse<T> response,
        HttpContext httpContext,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (response.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess(response.Result!)
                : new OkObjectResult(response.Result);
        }

        var (statusCode, title) = response.Status switch
        {
            UseCaseStatus.InvalidRequest => (StatusCodes.Status400BadRequest, "Invalid request"),
            UseCaseStatus.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UseCaseStatus.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            UseCaseStatus.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            UseCaseStatus.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred"),
        };

        var problem = _problemDetailsFactory.Create(
            httpContext,
            statusCode,
            title,
            response.Message,
            response.ErrorCode);

        return problem.ToObjectResult();
    }
}
