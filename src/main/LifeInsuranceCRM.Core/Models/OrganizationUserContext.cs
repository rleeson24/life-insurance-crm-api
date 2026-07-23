namespace LifeInsuranceCRM.Core.Models;

public sealed record OrganizationUserContext(Guid TenantId, string Role, bool IsActive);
