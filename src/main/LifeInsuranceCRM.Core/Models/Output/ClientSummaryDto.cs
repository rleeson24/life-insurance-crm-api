namespace LifeInsuranceCRM.Core.Models.Output;

public sealed class ClientSummaryDto
{
    public Guid ClientId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? LegalName { get; init; }
    public string? PrimaryPhone { get; init; }
    public bool IsActive { get; init; }
    public bool IsAcaClient { get; init; }
    public string? ActivePlanName { get; init; }
    public DateTimeOffset? LastContactedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
