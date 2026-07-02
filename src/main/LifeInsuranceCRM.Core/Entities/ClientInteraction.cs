namespace LifeInsuranceCRM.Core.Entities;

public sealed class ClientInteraction
{
    public Guid ClientInteractionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ClientId { get; init; }
    public DateTimeOffset ContactedAt { get; init; }
    public string? Summary { get; init; }
    public string? Notes { get; init; }
    public bool RequiresFollowUp { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid UpdatedByUserId { get; init; }
}
