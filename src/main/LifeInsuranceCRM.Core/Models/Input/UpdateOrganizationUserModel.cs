using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateOrganizationUserModel
{
    public Guid OrganizationUserId { get; init; }

    public string? EmailAddress { get; init; }

    public string? DisplayName { get; init; }

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}
