using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Services;

public static class SecurityAudit
{
    public const int StatusOk = 200;
    public const int StatusCreated = 201;
    public const int StatusNoContent = 204;
    public const int StatusBadRequest = 400;
    public const int StatusUnauthorized = 401;
    public const int StatusForbidden = 403;
    public const int StatusNotFound = 404;
    public const int StatusConflict = 409;
    public const int StatusTooManyRequests = 429;

    public static int FromStatus(UseCaseStatus status) => status switch
    {
        UseCaseStatus.Success => StatusOk,
        UseCaseStatus.InvalidRequest => StatusBadRequest,
        UseCaseStatus.Unauthorized => StatusUnauthorized,
        UseCaseStatus.Forbidden => StatusForbidden,
        UseCaseStatus.NotFound => StatusNotFound,
        UseCaseStatus.Conflict => StatusConflict,
        _ => 500,
    };

    public static Task RecordAsync(
        IAuthSecurityEventRecorder recorder,
        string eventType,
        bool success,
        string resource,
        CancellationToken cancellationToken,
        int? httpStatus = null,
        int? resultCount = null,
        Guid? targetId = null,
        string? detail = null,
        string? failureReason = null) =>
        recorder.RecordAsync(
            eventType,
            success,
            failureReason,
            resource,
            cancellationToken,
            httpStatus,
            resultCount,
            targetId,
            detail);
}
