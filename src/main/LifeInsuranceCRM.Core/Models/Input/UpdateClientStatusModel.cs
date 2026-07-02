namespace LifeInsuranceCRM.Core.Models.Input;

public sealed record UpdateClientStatusModel
{
    public Guid ClientId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsAcaClient { get; init; }
    public bool? HasContactConsent { get; init; }
}
