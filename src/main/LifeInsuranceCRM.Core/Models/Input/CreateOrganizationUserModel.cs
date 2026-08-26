using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record CreateOrganizationUserModel
{
    public Guid UserId { get; init; }

    public string? EmailAddress { get; init; }

    public string? DisplayName { get; init; }

    public string Role { get; init; } = OrganizationRoles.Agent;

    public Guid? TenantId { get; init; }
}
