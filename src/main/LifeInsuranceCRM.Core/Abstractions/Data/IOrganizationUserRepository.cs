using LifeInsuranceCRM.Core.Entities;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IOrganizationUserRepository
{
    Task<Guid?> GetTenantIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
