using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class ClientInteractionDto
{
    public Guid ClientInteractionId { get; init; }
    public Guid ClientId { get; init; }
    public DateTimeOffset ContactedAt { get; init; }
    public string? Summary { get; init; }
    public string? Notes { get; init; }
    public bool RequiresFollowUp { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
