using System.Security.Claims;
using System.Threading.RateLimiting;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeInsuranceCRM.API.RateLimiting;

public static class RateLimitingPolicyNames
{
    public const string AnonymousByIp = "AnonymousByIp";
    public const string AuthenticatedByUser = "AuthenticatedByUser";
    public const string SecuritySensitive = "SecuritySensitive";
    public const string AuthFailureByIp = "AuthFailureByIp";
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCrmRateLimiting(
        this IServiceCollection services,
        RateLimitingOptions options)
    {
        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var recorder = context.HttpContext.RequestServices.GetService<IAuthSecurityEventRecorder>();
                if (recorder is not null)
                {
                    await recorder.RecordAsync(
                        AuthSecurityEventTypes.RateLimitExceeded,
                        success: false,
                        failureReason: "Rate limit exceeded",
                        cancellationToken: cancellationToken);
                }
            };

            rateLimiterOptions.AddPolicy(RateLimitingPolicyNames.AnonymousByIp, httpContext =>
            {
                if (IsHealthPath(httpContext.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("health");
                }

                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.AnonymousRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    });
            });

            rateLimiterOptions.AddPolicy(RateLimitingPolicyNames.AuthenticatedByUser, httpContext =>
            {
                if (IsHealthPath(httpContext.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("health");
                }

                var userId = httpContext.User.FindFirstValue("oid")
                    ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    userId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.AuthenticatedRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    });
            });

            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (IsHealthPath(httpContext.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("health");
                }

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirstValue("oid")
                        ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "authenticated";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        userId,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.AuthenticatedRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true,
                        });
                }

                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.AnonymousRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    });
            });
        });

        return services;
    }

    private static bool IsHealthPath(PathString path) =>
        path.StartsWithSegments("/health") || path.StartsWithSegments("/alive");
}
