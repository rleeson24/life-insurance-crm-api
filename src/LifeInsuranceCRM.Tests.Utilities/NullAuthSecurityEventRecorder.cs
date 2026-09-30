using LifeInsuranceCRM.Core.Abstractions.Services;

namespace LifeInsuranceCRM.Tests.Utilities;

public sealed class NullAuthSecurityEventRecorder : IAuthSecurityEventRecorder
{
    public static NullAuthSecurityEventRecorder Instance { get; } = new();

    public Task RecordAsync(
        string eventType,
        bool success,
        string? failureReason = null,
        string? resource = null,
        CancellationToken cancellationToken = default,
        int? httpStatus = null,
        int? resultCount = null,
        Guid? targetId = null,
        string? detail = null) => Task.CompletedTask;
}
