using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListOrganizationUsersRequest
{
    public Guid? TenantId { get; init; }
}
