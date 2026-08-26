using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Output;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IOrganizationUserRepository
{
    Task<OrganizationUserContext?> GetUserContextAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationUserDto>> ListAsync(
        Guid? tenantId,
        bool includeSuperAdmins,
        CancellationToken cancellationToken = default);

    Task<OrganizationUserDto?> GetByOrganizationUserIdAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountActiveAdminsInTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default);

    Task<OrganizationUserDto> InsertAsync(
        Guid tenantId,
        Guid userId,
        string? emailAddress,
        string? displayName,
        string role,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<OrganizationUserDto?> UpdateAsync(
        Guid organizationUserId,
        string? emailAddress,
        string? displayName,
        string role,
        bool isActive,
        AuditStamp audit,
        CancellationToken cancellationToken = default);
}
