using LifeInsuranceCRM.API.Models;
using LifeInsuranceCRM.Core.Abstractions.Auth;
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
    private readonly IActorTracker _actorTracker;
    private readonly ILogger<ProcessResponseActionMapper> _logger;

    public ProcessResponseActionMapper(
        IProblemDetailsFactory problemDetailsFactory,
        IActorTracker actorTracker,
        ILogger<ProcessResponseActionMapper> logger)
    {
        _problemDetailsFactory = problemDetailsFactory;
        _actorTracker = actorTracker;
        _logger = logger;
    }

    public IActionResult Map<T>(
        ProcessResponse<T> response,
        HttpContext httpContext,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (response.IsSuccess)
        {
            LogSuccessIfMutating(httpContext);
            return onSuccess is not null
                ? onSuccess(response.Result!)
                : new OkObjectResult(response.Result);
        }

        LogFailure(httpContext, response.Status, response.ErrorCode, response.Message);

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

    private void LogSuccessIfMutating(HttpContext httpContext)
    {
        if (!IsMutating(httpContext.Request.Method))
        {
            return;
        }

        _logger.LogInformation(
            "Completed {HttpMethod} {Path} for user {UserId} tenant {TenantId}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            _actorTracker.UserId,
            _actorTracker.TenantId);
    }

    private void LogFailure(
        HttpContext httpContext,
        UseCaseStatus status,
        string? errorCode,
        string? message)
    {
        var level = status switch
        {
            UseCaseStatus.InvalidRequest => LogLevel.Debug,
            UseCaseStatus.NotFound or UseCaseStatus.Conflict => LogLevel.Information,
            _ => LogLevel.Warning,
        };

        _logger.Log(
            level,
            "Use case {Status} for {HttpMethod} {Path} with error {ErrorCode} for user {UserId} tenant {TenantId}: {Message}",
            status,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            errorCode,
            _actorTracker.UserId,
            _actorTracker.TenantId,
            message);
    }

    private static bool IsMutating(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method);
}
