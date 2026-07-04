using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateClientInteractionModel
{
    public Guid ClientId { get; init; }
    public Guid ClientInteractionId { get; init; }
    public DateTimeOffset ContactedAt { get; init; }
    public string? Summary { get; init; }
    public string? Notes { get; init; }
    public bool RequiresFollowUp { get; init; }
}
