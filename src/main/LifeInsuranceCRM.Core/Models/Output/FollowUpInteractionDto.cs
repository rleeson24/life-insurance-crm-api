using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class FollowUpInteractionDto
{
    public Guid ClientInteractionId { get; init; }
    public Guid ClientId { get; init; }
    public string? ClientFirstName { get; init; }
    public string? ClientLastName { get; init; }
    public DateTimeOffset ContactedAt { get; init; }
    public string? Summary { get; init; }
    public bool RequiresFollowUp { get; init; }
}
