using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IPlanNameRepository
{
    Task<IReadOnlyList<PlanName>> ListByYearAsync(
        PlanNameKind kind,
        short planYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanName>> ListByYearRangeAsync(
        PlanNameKind kind,
        short fromYear,
        short toYear,
        CancellationToken cancellationToken = default);

    Task<PlanName?> GetByIdAsync(
        PlanNameKind kind,
        Guid planNameId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        PlanNameKind kind,
        short planYear,
        string name,
        Guid? excludePlanNameId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountByYearAsync(
        PlanNameKind kind,
        short planYear,
        CancellationToken cancellationToken = default);

    Task<PlanName> InsertAsync(
        PlanNameKind kind,
        Guid tenantId,
        short planYear,
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<PlanName?> UpdateNameAsync(
        PlanNameKind kind,
        Guid planNameId,
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        PlanNameKind kind,
        Guid planNameId,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanName>> CloneYearAsync(
        PlanNameKind kind,
        Guid tenantId,
        short sourceYear,
        short targetYear,
        AuditStamp audit,
        CancellationToken cancellationToken = default);
}
