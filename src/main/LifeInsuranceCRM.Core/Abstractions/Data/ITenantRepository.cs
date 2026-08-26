using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Output;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface ITenantRepository
{
    Task<IReadOnlyList<TenantDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<TenantDto> InsertAsync(
        string name,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<TenantDto?> UpdateAsync(
        Guid tenantId,
        string? name,
        bool? isActive,
        AuditStamp audit,
        CancellationToken cancellationToken = default);
}
