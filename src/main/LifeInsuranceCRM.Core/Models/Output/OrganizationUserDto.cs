using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class OrganizationUserDto
{
    public Guid OrganizationUserId { get; init; }

    public Guid TenantId { get; init; }

    public string? TenantName { get; init; }

    public Guid UserId { get; init; }

    public string? EmailAddress { get; init; }

    public string? DisplayName { get; init; }

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
