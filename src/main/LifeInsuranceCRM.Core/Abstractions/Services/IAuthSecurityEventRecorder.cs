namespace LifeInsuranceCRM.Core.Abstractions.Services;

public interface IAuthSecurityEventRecorder
{
    Task RecordAsync(
        string eventType,
        bool success,
        string? failureReason = null,
        string? resource = null,
        CancellationToken cancellationToken = default,
        int? httpStatus = null,
        int? resultCount = null,
        Guid? targetId = null,
        string? detail = null);
}
