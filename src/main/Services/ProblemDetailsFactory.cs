using System.Diagnostics;
using LifeInsuranceCRM.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LifeInsuranceCRM.API.Services;

public interface IProblemDetailsFactory
{
    ProblemDetailsDto Create(
        HttpContext httpContext,
        int statusCode,
        string title,
        string? detail = null,
        string? errorCode = null);
}

public sealed class ProblemDetailsFactory : IProblemDetailsFactory
{
    public ProblemDetailsDto Create(
        HttpContext httpContext,
        int statusCode,
        string title,
        string? detail = null,
        string? errorCode = null)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        return new ProblemDetailsDto
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path.Value,
            TraceId = traceId,
            ErrorCode = errorCode,
        };
    }
}

public static class ProblemDetailsResultExtensions
{
    public static ObjectResult ToObjectResult(this ProblemDetailsDto problemDetails) =>
        new(problemDetails) { StatusCode = problemDetails.Status };
}
