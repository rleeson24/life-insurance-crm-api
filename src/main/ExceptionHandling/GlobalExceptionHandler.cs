using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Diagnostics;

namespace LifeInsuranceCRM.API.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsFactory _problemDetailsFactory;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsFactory problemDetailsFactory,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsFactory = problemDetailsFactory;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");

        var (status, title, errorCode) = exception switch
        {
            CrmException crm => (MapStatus(crm.Status), crm.Message, crm.ErrorCode),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "unexpected_error"),
        };

        var problem = _problemDetailsFactory.Create(httpContext, status, title, exception.Message, errorCode);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static int MapStatus(UseCaseStatus status) => status switch
    {
        UseCaseStatus.InvalidRequest => StatusCodes.Status400BadRequest,
        UseCaseStatus.Unauthorized => StatusCodes.Status401Unauthorized,
        UseCaseStatus.Forbidden => StatusCodes.Status403Forbidden,
        UseCaseStatus.NotFound => StatusCodes.Status404NotFound,
        UseCaseStatus.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };
}
