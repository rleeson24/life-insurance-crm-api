namespace LifeInsuranceCRM.Core.Models.Input;

public sealed record CreateClientInteractionModel
{
    public Guid ClientId { get; init; }
    public DateTimeOffset ContactedAt { get; init; }
    public string? Summary { get; init; }
    public string? Notes { get; init; }
    public bool RequiresFollowUp { get; init; }
}
