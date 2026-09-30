using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Abstractions.Services;

public interface ISecurityAudit
{
    int FromStatus(UseCaseStatus status);

    Task RecordAsync(
        string eventType,
        bool success,
        string resource,
        CancellationToken cancellationToken,
        int? httpStatus = null,
        int? resultCount = null,
        Guid? targetId = null,
        string? detail = null,
        string? failureReason = null);
}
