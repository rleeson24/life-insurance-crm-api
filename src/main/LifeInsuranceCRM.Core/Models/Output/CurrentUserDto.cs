using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class CurrentUserDto
{
    public Guid UserId { get; init; }

    public string? Email { get; init; }

    public Guid TenantId { get; init; }

    public string? TenantName { get; init; }

    public string Role { get; init; } = string.Empty;
}
