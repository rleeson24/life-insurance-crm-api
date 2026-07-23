using LifeInsuranceCRM.Core.Models;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IOrganizationUserRepository
{
    Task<OrganizationUserContext?> GetUserContextAsync(Guid userId, CancellationToken cancellationToken = default);
}
