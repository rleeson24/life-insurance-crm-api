using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Constants;
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
        _logger.LogError(PiiRedactor.ToSanitizedException(exception), "Unhandled exception");

        var (status, title, detail, errorCode) = exception switch
        {
            CrmException crm => (MapStatus(crm.Status), crm.Message, crm.Message, crm.ErrorCode),
            BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge => (
                StatusCodes.Status413PayloadTooLarge,
                "Payload too large",
                "The import file is larger than 20 MB",
                ImportErrorCodes.PayloadTooLarge),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "An unexpected error occurred.",
                "unexpected_error"),
        };

        var problem = _problemDetailsFactory.Create(httpContext, status, title, detail, errorCode);
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
