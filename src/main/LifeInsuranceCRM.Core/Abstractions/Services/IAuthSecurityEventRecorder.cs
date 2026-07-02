namespace LifeInsuranceCRM.Core.Abstractions.Services;

public interface IAuthSecurityEventRecorder
{
    Task RecordAsync(
        string eventType,
        bool success,
        string? failureReason = null,
        string? resource = null,
        CancellationToken cancellationToken = default);
}
